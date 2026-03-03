'use client';

import { Input, Button, Spin, Alert } from 'antd';
import { SendOutlined } from '@ant-design/icons';

interface MessageInputProps {
  value: string;
  onChange: (v: string) => void;
  onSend: () => void;
  onKeyDown: (e: React.KeyboardEvent) => void;
  sending: boolean;
  sendError: string;
}

export function MessageInput({ value, onChange, onSend, onKeyDown, sending, sendError }: MessageInputProps) {
  return (
    <div>
      {sendError && (
        <Alert type="error" message={sendError} showIcon style={{ marginBottom: 8 }} />
      )}
      <div style={{ display: 'flex', gap: 8 }}>
        <Input.TextArea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Type a message... (Enter to send)"
          autoSize={{ minRows: 1, maxRows: 4 }}
          style={{ flex: 1, resize: 'none' }}
          disabled={sending}
        />
        <Button
          type="primary"
          icon={sending ? <Spin size="small" /> : <SendOutlined />}
          onClick={onSend}
          disabled={!value.trim() || sending}
          style={{ height: 'auto', minWidth: 48 }}
        />
      </div>
    </div>
  );
}
