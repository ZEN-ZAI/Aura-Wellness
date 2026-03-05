package handlers

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/google/uuid"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/timestamppb"
)

// ConversationHandler handles all conversation-related gRPC RPCs.
type ConversationHandler struct {
	svc ports.ConversationService
}

func NewConversationHandler(svc ports.ConversationService) *ConversationHandler {
	return &ConversationHandler{svc: svc}
}

func (h *ConversationHandler) GetOrCreateDM(ctx context.Context, req *pb.GetOrCreateDMRequest) (*pb.Conversation, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	personA, err := uuid.Parse(req.PersonAId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_a_id: %v", err)
	}
	personB, err := uuid.Parse(req.PersonBId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_b_id: %v", err)
	}

	conv, err := h.svc.GetOrCreateDMConversation(ctx, wsID, personA, personB)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return conversationToProto(conv), nil
}

func (h *ConversationHandler) GetGroupConversation(ctx context.Context, req *pb.GetGroupConversationRequest) (*pb.Conversation, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}

	conv, err := h.svc.GetOrCreateGroupConversation(ctx, wsID)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return conversationToProto(conv), nil
}

func (h *ConversationHandler) ListConversations(ctx context.Context, req *pb.ListConversationsRequest) (*pb.ListConversationsResponse, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	pID, err := uuid.Parse(req.PersonId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_id: %v", err)
	}

	convs, err := h.svc.ListConversations(ctx, wsID, pID)
	if err != nil {
		return nil, toGrpcError(err)
	}

	protoConvs := make([]*pb.Conversation, len(convs))
	for i, c := range convs {
		protoConvs[i] = conversationToProto(c)
	}
	return &pb.ListConversationsResponse{Conversations: protoConvs}, nil
}

func (h *ConversationHandler) GetConversationParticipants(ctx context.Context, req *pb.GetConversationParticipantsRequest) (*pb.GetConversationParticipantsResponse, error) {
	convID, err := uuid.Parse(req.ConversationId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid conversation_id: %v", err)
	}

	participants, err := h.svc.GetConversationParticipants(ctx, convID)
	if err != nil {
		return nil, toGrpcError(err)
	}

	protoParticipants := make([]*pb.ConversationParticipant, len(participants))
	for i, p := range participants {
		protoParticipants[i] = &pb.ConversationParticipant{
			Id:             p.ID.String(),
			ConversationId: p.ConversationID.String(),
			PersonId:       p.PersonID.String(),
		}
	}
	return &pb.GetConversationParticipantsResponse{Participants: protoParticipants}, nil
}

func conversationToProto(c entities.ChatConversation) *pb.Conversation {
	return &pb.Conversation{
		Id:          c.ID.String(),
		WorkspaceId: c.WorkspaceID.String(),
		Type:        c.Type,
		CreatedAt:   timestamppb.New(c.CreatedAt),
	}
}
