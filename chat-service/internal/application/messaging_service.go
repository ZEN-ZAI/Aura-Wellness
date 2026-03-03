package application

import (
	"context"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/google/uuid"
)

type MessagingService struct {
	messageRepo ports.MessageRepository
	memberRepo  ports.MemberRepository
	pubsub      ports.PubSub
}

func NewMessagingService(mr ports.MessageRepository, mem ports.MemberRepository, ps ports.PubSub) *MessagingService {
	return &MessagingService{messageRepo: mr, memberRepo: mem, pubsub: ps}
}

// SendMessage verifies the caller has chat access, persists the message, and
// fire-and-forgets a Redis publish so Redis failures never block the RPC.
func (s *MessagingService) SendMessage(ctx context.Context, workspaceID, personID uuid.UUID, senderName, content string) (entities.ChatMessage, error) {
	member, err := s.memberRepo.GetByWorkspaceAndPerson(ctx, workspaceID, personID)
	if err != nil {
		return entities.ChatMessage{}, err
	}
	if !member.HasAccess {
		return entities.ChatMessage{}, ErrChatAccessDenied
	}

	msg, err := entities.NewMessage(workspaceID, personID, senderName, content)
	if err != nil {
		return entities.ChatMessage{}, err
	}

	saved, err := s.messageRepo.Save(ctx, msg)
	if err != nil {
		return entities.ChatMessage{}, err
	}

	go func() {
		_ = s.pubsub.Publish(context.Background(), workspaceID.String(), ports.MessageEvent{Message: saved})
	}()

	return saved, nil
}

func (s *MessagingService) ListMessages(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error) {
	return s.messageRepo.List(ctx, workspaceID, before, limit)
}

// StreamMessages subscribes to the workspace Pub/Sub channel and returns a
// typed channel of ChatMessages. The caller must invoke the returned cleanup
// function when done (e.g. via defer) to release the subscription.
func (s *MessagingService) StreamMessages(ctx context.Context, workspaceID uuid.UUID) (<-chan entities.ChatMessage, func(), error) {
	eventCh, cleanup, err := s.pubsub.Subscribe(ctx, workspaceID.String())
	if err != nil {
		return nil, nil, err
	}

	msgCh := make(chan entities.ChatMessage)
	go func() {
		defer close(msgCh)
		for {
			select {
			case <-ctx.Done():
				return
			case event, ok := <-eventCh:
				if !ok {
					return
				}
				select {
				case msgCh <- event.Message:
				case <-ctx.Done():
					return
				}
			}
		}
	}()

	return msgCh, cleanup, nil
}
