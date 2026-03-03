package application_test

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/aura-wellness/chat-service/internal/application"
	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/mocks"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/google/uuid"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

// ── SendMessage ───────────────────────────────────────────────────────────────

func TestSendMessage_MemberNotFound_ReturnsError(t *testing.T) {
	repoErr := errors.New("member not found")
	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{},
		&mocks.MockMemberRepository{
			GetByWorkspaceAndPersonFn: func(_ context.Context, _, _ uuid.UUID) (entities.ChatWorkspaceMember, error) {
				return entities.ChatWorkspaceMember{}, repoErr
			},
		},
		&mocks.MockPubSub{},
	)

	wsID := uuid.New()
	pID := uuid.New()
	_, err := svc.SendMessage(context.Background(), wsID, pID, "Alice", "Hello")

	assert.ErrorIs(t, err, repoErr)
}

func TestSendMessage_NoAccess_ReturnsErrChatAccessDenied(t *testing.T) {
	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{},
		&mocks.MockMemberRepository{
			GetByWorkspaceAndPersonFn: func(_ context.Context, _, _ uuid.UUID) (entities.ChatWorkspaceMember, error) {
				return entities.ChatWorkspaceMember{HasAccess: false}, nil
			},
		},
		&mocks.MockPubSub{},
	)

	_, err := svc.SendMessage(context.Background(), uuid.New(), uuid.New(), "Alice", "Hello")

	assert.ErrorIs(t, err, application.ErrChatAccessDenied)
}

func TestSendMessage_SaveError_ReturnsError(t *testing.T) {
	saveErr := errors.New("db error")
	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{
			SaveFn: func(_ context.Context, _ entities.ChatMessage) (entities.ChatMessage, error) {
				return entities.ChatMessage{}, saveErr
			},
		},
		&mocks.MockMemberRepository{
			GetByWorkspaceAndPersonFn: func(_ context.Context, _, _ uuid.UUID) (entities.ChatWorkspaceMember, error) {
				return entities.ChatWorkspaceMember{HasAccess: true}, nil
			},
		},
		&mocks.MockPubSub{
			PublishFn: func(_ context.Context, _ string, _ ports.MessageEvent) error { return nil },
		},
	)

	_, err := svc.SendMessage(context.Background(), uuid.New(), uuid.New(), "Alice", "Hello")

	assert.ErrorIs(t, err, saveErr)
}

func TestSendMessage_Success_ReturnsMessage(t *testing.T) {
	wsID := uuid.New()
	pID := uuid.New()
	publishCalled := make(chan struct{}, 1)

	savedMsg := entities.ChatMessage{
		ID:          uuid.New(),
		WorkspaceID: wsID,
		PersonID:    pID,
		SenderName:  "Alice",
		Content:     "Hello",
		CreatedAt:   time.Now().UTC(),
	}

	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{
			SaveFn: func(_ context.Context, m entities.ChatMessage) (entities.ChatMessage, error) {
				return savedMsg, nil
			},
		},
		&mocks.MockMemberRepository{
			GetByWorkspaceAndPersonFn: func(_ context.Context, _, _ uuid.UUID) (entities.ChatWorkspaceMember, error) {
				return entities.ChatWorkspaceMember{HasAccess: true}, nil
			},
		},
		&mocks.MockPubSub{
			PublishFn: func(_ context.Context, _ string, _ ports.MessageEvent) error {
				publishCalled <- struct{}{}
				return nil
			},
		},
	)

	got, err := svc.SendMessage(context.Background(), wsID, pID, "Alice", "Hello")

	require.NoError(t, err)
	assert.Equal(t, savedMsg.ID, got.ID)
	assert.Equal(t, "Hello", got.Content)

	// Publish is fire-and-forget; wait briefly for the goroutine
	select {
	case <-publishCalled:
	case <-time.After(200 * time.Millisecond):
		t.Error("expected pubsub.Publish to be called asynchronously")
	}
}

// ── ListMessages ──────────────────────────────────────────────────────────────

func TestListMessages_DelegatesToRepo(t *testing.T) {
	wsID := uuid.New()
	before := time.Now().UTC()
	limit := 10
	expected := []entities.ChatMessage{{ID: uuid.New(), Content: "hi"}}

	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{
			ListFn: func(_ context.Context, w uuid.UUID, b time.Time, l int) ([]entities.ChatMessage, error) {
				assert.Equal(t, wsID, w)
				assert.Equal(t, before, b)
				assert.Equal(t, limit, l)
				return expected, nil
			},
		},
		&mocks.MockMemberRepository{},
		&mocks.MockPubSub{},
	)

	got, err := svc.ListMessages(context.Background(), wsID, before, limit)

	require.NoError(t, err)
	assert.Equal(t, expected, got)
}

// ── StreamMessages ────────────────────────────────────────────────────────────

func TestStreamMessages_SubscribeError_ReturnsError(t *testing.T) {
	subErr := errors.New("redis down")

	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{},
		&mocks.MockMemberRepository{},
		&mocks.MockPubSub{
			SubscribeFn: func(_ context.Context, _ string) (<-chan ports.MessageEvent, func(), error) {
				return nil, nil, subErr
			},
		},
	)

	ch, cleanup, err := svc.StreamMessages(context.Background(), uuid.New())

	assert.ErrorIs(t, err, subErr)
	assert.Nil(t, ch)
	assert.Nil(t, cleanup)
}

func TestStreamMessages_ForwardsMessagesFromPubSub(t *testing.T) {
	eventCh := make(chan ports.MessageEvent, 1)
	msg := entities.ChatMessage{ID: uuid.New(), Content: "streamed"}
	eventCh <- ports.MessageEvent{Message: msg}

	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{},
		&mocks.MockMemberRepository{},
		&mocks.MockPubSub{
			SubscribeFn: func(_ context.Context, _ string) (<-chan ports.MessageEvent, func(), error) {
				return eventCh, func() {}, nil
			},
		},
	)

	ctx, cancel := context.WithTimeout(context.Background(), time.Second)
	defer cancel()

	ch, cleanup, err := svc.StreamMessages(ctx, uuid.New())
	require.NoError(t, err)
	defer cleanup()

	select {
	case got := <-ch:
		assert.Equal(t, msg.ID, got.ID)
		assert.Equal(t, msg.Content, got.Content)
	case <-ctx.Done():
		t.Fatal("timed out waiting for message")
	}
}

func TestStreamMessages_ContextCancel_ClosesChannel(t *testing.T) {
	eventCh := make(chan ports.MessageEvent) // never sends

	svc := application.NewMessagingService(
		&mocks.MockMessageRepository{},
		&mocks.MockMemberRepository{},
		&mocks.MockPubSub{
			SubscribeFn: func(_ context.Context, _ string) (<-chan ports.MessageEvent, func(), error) {
				return eventCh, func() {}, nil
			},
		},
	)

	ctx, cancel := context.WithCancel(context.Background())

	ch, cleanup, err := svc.StreamMessages(ctx, uuid.New())
	require.NoError(t, err)
	defer cleanup()

	cancel() // trigger context cancellation

	select {
	case _, ok := <-ch:
		assert.False(t, ok, "channel should be closed")
	case <-time.After(200 * time.Millisecond):
		t.Fatal("timed out waiting for channel to close")
	}
}
