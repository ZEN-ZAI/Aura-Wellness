package handlers

import (
	"context"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/google/uuid"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/timestamppb"
)

// MessagingHandler handles all message-related gRPC RPCs.
type MessagingHandler struct {
	svc ports.MessagingService
}

func NewMessagingHandler(svc ports.MessagingService) *MessagingHandler {
	return &MessagingHandler{svc: svc}
}

func (h *MessagingHandler) SendMessage(ctx context.Context, req *pb.SendMessageRequest) (*pb.ChatMessage, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	pID, err := uuid.Parse(req.PersonId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_id: %v", err)
	}
	msg, err := h.svc.SendMessage(ctx, wsID, pID, req.SenderName, req.Content)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return messageToProto(msg), nil
}

func (h *MessagingHandler) ListMessages(ctx context.Context, req *pb.ListMessagesRequest) (*pb.ListMessagesResponse, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}

	before := time.Now().UTC().Add(time.Second)
	if req.Before != nil && req.Before.IsValid() {
		before = req.Before.AsTime()
	}
	limit := int(req.Limit)
	if limit <= 0 || limit > 200 {
		limit = 50
	}

	msgs, err := h.svc.ListMessages(ctx, wsID, before, limit)
	if err != nil {
		return nil, toGrpcError(err)
	}
	protoMsgs := make([]*pb.ChatMessage, len(msgs))
	for i, m := range msgs {
		protoMsgs[i] = messageToProto(m)
	}
	return &pb.ListMessagesResponse{Messages: protoMsgs}, nil
}

// StreamMessages subscribes to the workspace channel and streams messages to the client.
// The handler delegates channel management entirely to the service layer.
func (h *MessagingHandler) StreamMessages(req *pb.StreamMessagesRequest, stream pb.ChatService_StreamMessagesServer) error {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}

	msgCh, cleanup, err := h.svc.StreamMessages(stream.Context(), wsID)
	if err != nil {
		return toGrpcError(err)
	}
	defer cleanup()

	for {
		select {
		case <-stream.Context().Done():
			return nil
		case msg, ok := <-msgCh:
			if !ok {
				return status.Error(codes.Unavailable, "subscription closed")
			}
			if err := stream.Send(messageToProto(msg)); err != nil {
				return err
			}
		}
	}
}

func messageToProto(m entities.ChatMessage) *pb.ChatMessage {
	return &pb.ChatMessage{
		Id:          m.ID.String(),
		WorkspaceId: m.WorkspaceID.String(),
		PersonId:    m.PersonID.String(),
		SenderName:  m.SenderName,
		Content:     m.Content,
		CreatedAt:   timestamppb.New(m.CreatedAt),
	}
}
