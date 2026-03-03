package handlers

import (
	"context"
	"errors"

	"github.com/aura-wellness/chat-service/internal/application"
	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/timestamppb"
)

// WorkspaceHandler handles all workspace-related gRPC RPCs.
// It is thin: parse UUIDs, call service, convert to proto, map errors.
type WorkspaceHandler struct {
	svc ports.WorkspaceService
}

func NewWorkspaceHandler(svc ports.WorkspaceService) *WorkspaceHandler {
	return &WorkspaceHandler{svc: svc}
}

func (h *WorkspaceHandler) CreateWorkspace(ctx context.Context, req *pb.CreateWorkspaceRequest) (*pb.Workspace, error) {
	buID, err := uuid.Parse(req.BuId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid bu_id: %v", err)
	}
	companyID, err := uuid.Parse(req.CompanyId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid company_id: %v", err)
	}
	w, err := h.svc.CreateWorkspace(ctx, buID, companyID, req.Name)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return workspaceToProto(w), nil
}

func (h *WorkspaceHandler) GetWorkspaceByBuId(ctx context.Context, req *pb.GetWorkspaceByBuIdRequest) (*pb.Workspace, error) {
	buID, err := uuid.Parse(req.BuId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid bu_id: %v", err)
	}
	w, err := h.svc.GetByBuID(ctx, buID)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return workspaceToProto(w), nil
}

func (h *WorkspaceHandler) GetWorkspaceById(ctx context.Context, req *pb.GetWorkspaceByIdRequest) (*pb.Workspace, error) {
	id, err := uuid.Parse(req.Id)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid id: %v", err)
	}
	w, err := h.svc.GetByID(ctx, id)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return workspaceToProto(w), nil
}

func (h *WorkspaceHandler) AddMember(ctx context.Context, req *pb.AddMemberRequest) (*pb.WorkspaceMember, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	pID, err := uuid.Parse(req.PersonId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_id: %v", err)
	}
	m, err := h.svc.AddMember(ctx, wsID, pID, req.Role)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return memberToProto(m), nil
}

func (h *WorkspaceHandler) UpdateMemberAccess(ctx context.Context, req *pb.UpdateMemberAccessRequest) (*pb.WorkspaceMember, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	pID, err := uuid.Parse(req.PersonId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid person_id: %v", err)
	}
	m, err := h.svc.UpdateMemberAccess(ctx, wsID, pID, req.HasAccess)
	if err != nil {
		return nil, toGrpcError(err)
	}
	return memberToProto(m), nil
}

func (h *WorkspaceHandler) ListMembers(ctx context.Context, req *pb.ListMembersRequest) (*pb.ListMembersResponse, error) {
	wsID, err := uuid.Parse(req.WorkspaceId)
	if err != nil {
		return nil, status.Errorf(codes.InvalidArgument, "invalid workspace_id: %v", err)
	}
	members, err := h.svc.ListMembers(ctx, wsID)
	if err != nil {
		return nil, toGrpcError(err)
	}
	protoMembers := make([]*pb.WorkspaceMember, len(members))
	for i, m := range members {
		protoMembers[i] = memberToProto(m)
	}
	return &pb.ListMembersResponse{Members: protoMembers}, nil
}

// ─── Proto conversions ────────────────────────────────────────────────────────

func workspaceToProto(w entities.ChatWorkspace) *pb.Workspace {
	return &pb.Workspace{
		Id:        w.ID.String(),
		BuId:      w.BuID.String(),
		CompanyId: w.CompanyID.String(),
		Name:      w.Name,
		CreatedAt: timestamppb.New(w.CreatedAt),
	}
}

func memberToProto(m entities.ChatWorkspaceMember) *pb.WorkspaceMember {
	return &pb.WorkspaceMember{
		Id:          m.ID.String(),
		WorkspaceId: m.WorkspaceID.String(),
		PersonId:    m.PersonID.String(),
		Role:        m.Role,
		HasAccess:   m.HasAccess,
		CreatedAt:   timestamppb.New(m.CreatedAt),
	}
}

// toGrpcError maps domain/application errors to gRPC status errors.
func toGrpcError(err error) error {
	if errors.Is(err, pgx.ErrNoRows) {
		return status.Error(codes.NotFound, "not found")
	}
	if errors.Is(err, application.ErrChatAccessDenied) {
		return status.Error(codes.PermissionDenied, "chat access denied")
	}
	return status.Errorf(codes.Internal, "%v", err)
}
