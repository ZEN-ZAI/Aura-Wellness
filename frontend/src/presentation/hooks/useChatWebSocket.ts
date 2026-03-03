import { useEffect, useRef, useCallback } from 'react';
import { useChatStore } from '@/application/stores/chatStore';
import type { ChatMessage } from '@/domain/entities/ChatMessage';

type WsOutgoing = { type: 'send_message'; content: string };

interface UseChatWebSocketResult {
  /** Send a chat message through the WebSocket connection. */
  sendMessage: (content: string) => void;
  /** Whether the socket is currently OPEN. */
  isConnected: () => boolean;
}

/**
 * Manages a bidirectional WebSocket connection for a chat workspace.
 *
 * - On mount (or when buId changes) it opens ws://<host>/api/chat/ws/<buId>.
 *   The custom Next.js server (/server.ts) proxies the connection to the
 *   .NET backend, injecting the Authorization header from the httpOnly cookie.
 * - Incoming JSON frames are mapped to ChatMessage and appended to the store.
 * - Reconnects automatically after 3 seconds on unexpected closure.
 * - Closes cleanly on unmount (code 1000 — suppresses auto-reconnect).
 */
export function useChatWebSocket(buId: string): UseChatWebSocketResult {
  const wsRef        = useRef<WebSocket | null>(null);
  const buIdRef      = useRef(buId);
  buIdRef.current    = buId;

  // Keep a stable reference to appendMessage so it never causes effect re-runs.
  const appendMessage = useChatStore((s) => s.appendMessage);

  useEffect(() => {
    if (!buId) return;

    let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
    let intentionalClose = false;

    function connect() {
      const ws = new WebSocket(`/api/chat/ws/${buId}`);
      wsRef.current = ws;

      ws.onopen = () => {
        if (reconnectTimer) {
          clearTimeout(reconnectTimer);
          reconnectTimer = null;
        }
      };

      ws.onmessage = (e) => {
        try {
          // Server sends ChatMessageServiceDto (camelCase):
          // { id, workspaceId, personId, senderName, content, createdAt }
          const raw = JSON.parse(e.data as string) as {
            id: string;
            personId: string;
            senderName: string;
            content: string;
            createdAt: string;
          };
          const msg: ChatMessage = {
            messageId:  raw.id,
            personId:   raw.personId,
            senderName: raw.senderName,
            content:    raw.content,
            createdAt:  raw.createdAt,
          };
          appendMessage(buIdRef.current, msg);
        } catch {
          // Ignore malformed frames.
        }
      };

      ws.onclose = (e) => {
        wsRef.current = null;
        if (!intentionalClose && e.code !== 1000) {
          // Reconnect after 3 s on unexpected closure.
          reconnectTimer = setTimeout(connect, 3_000);
        }
      };

      ws.onerror = () => {
        // onclose fires immediately after onerror — reconnect logic lives there.
        ws.close();
      };
    }

    connect();

    return () => {
      intentionalClose = true;
      if (reconnectTimer) clearTimeout(reconnectTimer);
      const ws = wsRef.current;
      if (ws) {
        ws.onclose = null; // Prevent reconnect during cleanup
        ws.close(1000, 'Component unmounted');
        wsRef.current = null;
      }
    };
  }, [buId]); // eslint-disable-line react-hooks/exhaustive-deps

  const sendMessage = useCallback((content: string) => {
    const ws = wsRef.current;
    if (!ws || ws.readyState !== WebSocket.OPEN) {
      throw new Error('WebSocket is not connected.');
    }
    const msg: WsOutgoing = { type: 'send_message', content };
    ws.send(JSON.stringify(msg));
  }, []);

  const isConnected = useCallback(
    () => wsRef.current?.readyState === WebSocket.OPEN,
    [],
  );

  return { sendMessage, isConnected };
}
