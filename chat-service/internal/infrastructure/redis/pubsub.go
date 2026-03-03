package redis

import (
	"context"
	"encoding/json"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/redis/go-redis/v9"
)

type redisPubSub struct {
	client *redis.Client
}

// NewPubSub creates a PubSub adapter backed by Redis.
func NewPubSub(addr string) ports.PubSub {
	return &redisPubSub{
		client: redis.NewClient(&redis.Options{Addr: addr}),
	}
}

func channelKey(workspaceID string) string {
	return fmt.Sprintf("chat:%s", workspaceID)
}

func (r *redisPubSub) Publish(ctx context.Context, workspaceID string, event ports.MessageEvent) error {
	payload, err := json.Marshal(event)
	if err != nil {
		return err
	}
	return r.client.Publish(ctx, channelKey(workspaceID), payload).Err()
}

// Subscribe returns a channel of MessageEvents and a cleanup function.
// Redis types are fully contained here; callers only see the domain port types.
func (r *redisPubSub) Subscribe(ctx context.Context, workspaceID string) (<-chan ports.MessageEvent, func(), error) {
	sub := r.client.Subscribe(ctx, channelKey(workspaceID))

	outCh := make(chan ports.MessageEvent, 16)
	go func() {
		defer close(outCh)
		redisCh := sub.Channel()
		for {
			select {
			case <-ctx.Done():
				return
			case msg, ok := <-redisCh:
				if !ok {
					return
				}
				var event ports.MessageEvent
				if err := json.Unmarshal([]byte(msg.Payload), &event); err != nil {
					continue
				}
				select {
				case outCh <- event:
				case <-ctx.Done():
					return
				}
			}
		}
	}()

	cleanup := func() { _ = sub.Close() }
	return outCh, cleanup, nil
}
