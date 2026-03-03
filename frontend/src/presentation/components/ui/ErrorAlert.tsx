'use client';

import { Alert } from 'antd';

export function ErrorAlert({ message, className }: { message: string; className?: string }) {
  return <Alert message={message} type="error" showIcon className={className} />;
}
