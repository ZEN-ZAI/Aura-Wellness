package mocks

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/ports"
)

// MockPubSub is a hand-written test double for ports.PubSub.
type MockPubSub struct {
	PublishFn   func(ctx context.Context, channel string, event ports.MessageEvent) error
	SubscribeFn func(ctx context.Context, channel string) (<-chan ports.MessageEvent, func(), error)
}

func (m *MockPubSub) Publish(ctx context.Context, channel string, event ports.MessageEvent) error {
	return m.PublishFn(ctx, channel, event)
}

func (m *MockPubSub) Subscribe(ctx context.Context, channel string) (<-chan ports.MessageEvent, func(), error) {
	return m.SubscribeFn(ctx, channel)
}
