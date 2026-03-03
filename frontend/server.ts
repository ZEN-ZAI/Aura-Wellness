/**
 * Custom Next.js server that proxies WebSocket connections to the .NET backend.
 *
 * Why a custom server?
 * - The chat JWT is stored in an httpOnly cookie (JS cannot read it).
 * - Next.js App Router route handlers do not support WebSocket upgrades.
 * - This server reads the cookie server-side, then opens a proxied WebSocket
 *   to the .NET backend with the Authorization header attached.
 *
 * Path handled: /api/chat/ws/:buId
 * All other requests are handled by Next.js normally.
 */

import { createServer } from 'http';
import { parse } from 'url';
import next from 'next';
import { WebSocketServer, WebSocket } from 'ws';
import { parse as parseCookies } from 'cookie';

const dev  = process.env.NODE_ENV !== 'production';
const port = parseInt(process.env.PORT ?? '3000', 10);

// In Docker the backend is reachable at http://backend:8080; locally at http://localhost:5239
const BACKEND_URL = process.env.BACKEND_URL ?? 'http://localhost:5239';
const BACKEND_WS  = BACKEND_URL.replace(/^http/, 'ws');

const WS_PATH_RE = /^\/api\/chat\/ws\/([0-9a-f-]{36})$/i;

const app    = next({ dev });
const handle = app.getRequestHandler();

app.prepare().then(() => {
  const server = createServer((req, res) => {
    const parsedUrl = parse(req.url!, true);
    handle(req, res, parsedUrl);
  });

  const wss = new WebSocketServer({ noServer: true });

  server.on('upgrade', (req, socket, head) => {
    const { pathname } = parse(req.url ?? '');
    const match = pathname?.match(WS_PATH_RE);

    if (!match) {
      socket.destroy();
      return;
    }

    const buId = match[1];

    // Read the httpOnly JWT cookie
    const cookies = parseCookies(req.headers.cookie ?? '');
    const token   = cookies['auth_token'];

    if (!token) {
      socket.write('HTTP/1.1 401 Unauthorized\r\n\r\n');
      socket.destroy();
      return;
    }

    wss.handleUpgrade(req, socket, head, (browserWs) => {
      const backendUrl = `${BACKEND_WS}/api/chat/workspace/${buId}/ws`;

      const backendWs = new WebSocket(backendUrl, {
        headers: { Authorization: `Bearer ${token}` },
      });

      backendWs.on('open', () => {
        // Browser → Backend
        browserWs.on('message', (data) => {
          if (backendWs.readyState === WebSocket.OPEN) {
            backendWs.send(data);
          }
        });

        // Backend → Browser
        backendWs.on('message', (data) => {
          if (browserWs.readyState === WebSocket.OPEN) {
            browserWs.send(data);
          }
        });

        browserWs.on('close', () => backendWs.close(1000));
        backendWs.on('close', () => {
          if (browserWs.readyState === WebSocket.OPEN) {
            browserWs.close(1000);
          }
        });
      });

      backendWs.on('error', (err) => {
        console.error('[ws-proxy] backend error:', err.message);
        if (browserWs.readyState === WebSocket.OPEN) {
          browserWs.close(1011, 'Backend error');
        }
      });

      browserWs.on('error', (err) => {
        console.error('[ws-proxy] browser error:', err.message);
        if (backendWs.readyState === WebSocket.OPEN) {
          backendWs.close(1011);
        }
      });
    });
  });

  server.listen(port, () => {
    console.log(`> Next.js ready on http://localhost:${port} [${dev ? 'dev' : 'prod'}]`);
  });
});
