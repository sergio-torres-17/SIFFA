<?php
declare(strict_types=1);

namespace Siffa\Core;

/** Plantillas PHP simples: views/pages/{nombre}.php dentro de views/layouts/{layout}.php */
final class View
{
    public static function render(string $page, array $data = [], string $layout = 'app'): void
    {
        $file = BASE_PATH . '/views/pages/' . $page . '.php';
        if (!is_file($file)) {
            throw new \RuntimeException("Vista no encontrada: {$page}");
        }

        extract($data, EXTR_SKIP);
        ob_start();
        require $file;
        $content = ob_get_clean();

        $title = $data['title'] ?? 'SIFFA';
        require BASE_PATH . '/views/layouts/' . $layout . '.php';
    }
}
