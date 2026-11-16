<?php
declare(strict_types=1);

namespace Siffa\Core;

/** Enrutador mínimo: coincidencia exacta de método + ruta. */
final class Router
{
    /** @var array<string,array<string,array{0:callable,1:bool}>> */
    private array $routes = [];

    /** @param bool $public true = no exige sesión iniciada */
    public function add(string $method, string $path, callable $handler, bool $public = false): void
    {
        $this->routes[strtoupper($method)][$path] = [$handler, $public];
    }

    public function get(string $path, callable $handler, bool $public = false): void
    {
        $this->add('GET', $path, $handler, $public);
    }

    public function post(string $path, callable $handler, bool $public = false): void
    {
        $this->add('POST', $path, $handler, $public);
    }

    public function dispatch(string $method, string $path): void
    {
        $route = $this->routes[$method][$path] ?? null;

        if ($route === null) {
            // ¿Existe la ruta con otro método?
            foreach ($this->routes as $m => $paths) {
                if (isset($paths[$path])) {
                    http_response_code(405);
                    header('Allow: ' . $m);
                    $this->fail('Método no permitido.', 405);
                }
            }
            $this->fail('Página no encontrada.', 404);
        }

        [$handler, $public] = $route;

        if (!$public && !Auth::check()) {
            if (Http::wantsJson()) {
                Http::json(['isSuccess' => false, 'message' => 'Tu sesión expiró. Vuelve a iniciar sesión.'], 401);
            }
            Http::redirect('/login');
        }

        // Todas las peticiones que modifican datos exigen el token CSRF, salvo la API para clientes
        // (escritorio) que se autentica con usuario y contraseña en cada llamada.
        if ($method === 'POST' && !str_starts_with($path, '/api/') && !Http::checkCsrf()) {
            if (Http::wantsJson()) {
                Http::json(['isSuccess' => false, 'message' => 'La página caducó. Recárgala e intenta de nuevo.'], 419);
            }
            Http::flash('error', 'La página caducó. Intenta de nuevo.');
            Http::redirect($public ? '/login' : '/');
        }

        $handler();
    }

    private function fail(string $message, int $status): never
    {
        http_response_code($status);
        if (Http::wantsJson()) {
            Http::json(['isSuccess' => false, 'message' => $message], $status);
        }
        View::render('error', ['title' => $status === 404 ? 'No encontrada' : 'Error', 'code' => $status, 'message' => $message], 'auth');
        exit;
    }
}
