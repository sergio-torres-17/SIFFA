<?php
declare(strict_types=1);

namespace Siffa\Core;

/**
 * Bitácora de la aplicación: storage/logs/siffa-AAAA-MM-DD.log (un archivo por día).
 * Los errores de PHP siguen yendo a storage/logs/php-error.log.
 */
final class Log
{
    private const KEEP_DAYS = 30;

    public static function info(string $message, array $context = []): void
    {
        self::write('INFO', $message, $context);
    }

    public static function warning(string $message, array $context = []): void
    {
        self::write('WARN', $message, $context);
    }

    public static function error(string $message, array $context = [], ?\Throwable $e = null): void
    {
        if ($e !== null) {
            $context['exception'] = get_class($e) . ': ' . $e->getMessage() . ' en ' . basename($e->getFile()) . ':' . $e->getLine();
            $context['trace'] = $e->getTraceAsString();
        }
        self::write('ERROR', $message, $context);
    }

    /** Registra también los errores fatales (memoria, tiempo agotado…) que no pasan por try/catch. */
    public static function registerFatalHandler(): void
    {
        register_shutdown_function(static function (): void {
            $err = error_get_last();
            if ($err !== null && in_array($err['type'], [E_ERROR, E_PARSE, E_CORE_ERROR, E_COMPILE_ERROR, E_USER_ERROR], true)) {
                Log::error('Error fatal de PHP', [
                    'tipo' => $err['type'],
                    'mensaje' => $err['message'],
                    'archivo' => basename($err['file']) . ':' . $err['line'],
                    'url' => ($_SERVER['REQUEST_METHOD'] ?? '') . ' ' . ($_SERVER['REQUEST_URI'] ?? ''),
                    'memoria_pico_mb' => round(memory_get_peak_usage(true) / 1048576, 1),
                ]);
            }
        });
    }

    private static function write(string $level, string $message, array $context): void
    {
        try {
            $dir = BASE_PATH . '/storage/logs';
            if (!is_dir($dir) && !@mkdir($dir, 0775, true) && !is_dir($dir)) {
                return;
            }

            $line = sprintf(
                "[%s] %s %s%s\n",
                date('Y-m-d H:i:s'),
                $level,
                $message,
                $context ? ' ' . json_encode($context, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PARTIAL_OUTPUT_ON_ERROR) : ''
            );
            @file_put_contents($dir . '/siffa-' . date('Y-m-d') . '.log', $line, FILE_APPEND | LOCK_EX);

            // Limpieza ocasional de bitácoras viejas.
            if (random_int(1, 200) === 1) {
                foreach (glob($dir . '/siffa-*.log') ?: [] as $file) {
                    if (filemtime($file) < time() - self::KEEP_DAYS * 86400) {
                        @unlink($file);
                    }
                }
            }
        } catch (\Throwable) {
            // El log nunca debe romper la aplicación.
        }
    }
}
