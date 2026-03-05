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

// In Docker the Go chat service WebSocket endpoint is at http://chat-service:8080;
// locally at http://localhost:8080 (start the chat service with WS_PORT=8080)
const CHAT_SERVICE_URL = process.env.CHAT_SERVICE_URL ?? 'http://localhost:8080';
const CHAT_WS  = CHAT_SERVICE_URL.replace(/^http/, 'ws');

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
      const backendUrl = `${CHAT_WS}/ws/${buId}`;

      const backendWs = new WebSocket(backendUrl, {
        headers: { Authorization: `Bearer ${token}` },
      });

      backendWs.on('open', () => {
        // Browser → Backend
        browserWs.on('message', (data) => {
          if (backendWs.readyState === WebSocket.OPEN) {
            backendWs.send(data.toString());
          }
        });

        // Backend → Browser
        // Convert Buffer to string so ws sends a text frame (opcode 1) instead
        // of a binary frame (opcode 2).  The browser WebSocket API delivers text
        // frames as strings to onmessage, which the frontend JSON.parse expects.
        // Without this, the browser receives a Blob and JSON.parse silently fails.
        backendWs.on('message', (data) => {
          if (browserWs.readyState === WebSocket.OPEN) {
            browserWs.send(data.toString());
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
