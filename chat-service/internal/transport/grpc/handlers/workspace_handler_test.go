package handlers_test

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/transport/grpc/handlers"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

// ── Mock WorkspaceService ─────────────────────────────────────────────────────

type mockWorkspaceSvc struct {
	CreateWorkspaceFn   func(ctx context.Context, buID, companyID uuid.UUID, name string) (entities.ChatWorkspace, error)
	GetByBuIDFn         func(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error)
	GetByIDFn           func(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error)
	AddMemberFn         func(ctx context.Context, workspaceID, personID uuid.UUID, role string) (entities.ChatWorkspaceMember, error)
	UpdateMemberAccessFn func(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error)
	ListMembersFn       func(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error)
}

func (m *mockWorkspaceSvc) CreateWorkspace(ctx context.Context, buID, companyID uuid.UUID, name string) (entities.ChatWorkspace, error) {
	return m.CreateWorkspaceFn(ctx, buID, companyID, name)
}
func (m *mockWorkspaceSvc) GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error) {
	return m.GetByBuIDFn(ctx, buID)
}
func (m *mockWorkspaceSvc) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error) {
	return m.GetByIDFn(ctx, id)
}
func (m *mockWorkspaceSvc) AddMember(ctx context.Context, workspaceID, personID uuid.UUID, role string) (entities.ChatWorkspaceMember, error) {
	return m.AddMemberFn(ctx, workspaceID, personID, role)
}
func (m *mockWorkspaceSvc) UpdateMemberAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error) {
	return m.UpdateMemberAccessFn(ctx, workspaceID, personID, hasAccess)
}
func (m *mockWorkspaceSvc) ListMembers(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
	return m.ListMembersFn(ctx, workspaceID)
}

// ── CreateWorkspace ───────────────────────────────────────────────────────────

func TestCreateWorkspace_InvalidBuUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{})
	_, err := h.CreateWorkspace(context.Background(), &pb.CreateWorkspaceRequest{
		BuId:      "not-a-uuid",
		CompanyId: uuid.New().String(),
		Name:      "Test",
	})

	require.Error(t, err)
	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestCreateWorkspace_InvalidCompanyUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{})
	_, err := h.CreateWorkspace(context.Background(), &pb.CreateWorkspaceRequest{
		BuId:      uuid.New().String(),
		CompanyId: "not-a-uuid",
		Name:      "Test",
	})

	require.Error(t, err)
	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestCreateWorkspace_Valid_ReturnsMappedWorkspace(t *testing.T) {
	buID := uuid.New()
	companyID := uuid.New()
	wsID := uuid.New()
	ws := entities.ChatWorkspace{ID: wsID, BuID: buID, CompanyID: companyID, Name: "Test", CreatedAt: time.Now()}

	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		CreateWorkspaceFn: func(_ context.Context, b, c uuid.UUID, n string) (entities.ChatWorkspace, error) {
			return ws, nil
		},
	})

	resp, err := h.CreateWorkspace(context.Background(), &pb.CreateWorkspaceRequest{
		BuId:      buID.String(),
		CompanyId: companyID.String(),
		Name:      "Test",
	})

	require.NoError(t, err)
	assert.Equal(t, wsID.String(), resp.Id)
	assert.Equal(t, "Test", resp.Name)
}

// ── GetWorkspaceByBuId ────────────────────────────────────────────────────────

func TestGetWorkspaceByBuId_InvalidUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{})
	_, err := h.GetWorkspaceByBuId(context.Background(), &pb.GetWorkspaceByBuIdRequest{BuId: "bad"})

	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestGetWorkspaceByBuId_NotFound_ReturnsNotFound(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		GetByBuIDFn: func(_ context.Context, _ uuid.UUID) (entities.ChatWorkspace, error) {
			return entities.ChatWorkspace{}, pgx.ErrNoRows
		},
	})

	_, err := h.GetWorkspaceByBuId(context.Background(), &pb.GetWorkspaceByBuIdRequest{
		BuId: uuid.New().String(),
	})

	assert.Equal(t, codes.NotFound, status.Code(err))
}

func TestGetWorkspaceByBuId_Valid_ReturnsMappedWorkspace(t *testing.T) {
	buID := uuid.New()
	ws := entities.ChatWorkspace{ID: uuid.New(), BuID: buID, Name: "BU Workspace", CreatedAt: time.Now()}

	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		GetByBuIDFn: func(_ context.Context, _ uuid.UUID) (entities.ChatWorkspace, error) {
			return ws, nil
		},
	})

	resp, err := h.GetWorkspaceByBuId(context.Background(), &pb.GetWorkspaceByBuIdRequest{
		BuId: buID.String(),
	})

	require.NoError(t, err)
	assert.Equal(t, ws.ID.String(), resp.Id)
	assert.Equal(t, "BU Workspace", resp.Name)
}

// ── AddMember ─────────────────────────────────────────────────────────────────

func TestAddMember_InvalidWorkspaceUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{})
	_, err := h.AddMember(context.Background(), &pb.AddMemberRequest{
		WorkspaceId: "bad",
		PersonId:    uuid.New().String(),
		Role:        "Member",
	})

	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestAddMember_ServiceError_ReturnsInternalError(t *testing.T) {
	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		AddMemberFn: func(_ context.Context, _, _ uuid.UUID, _ string) (entities.ChatWorkspaceMember, error) {
			return entities.ChatWorkspaceMember{}, errors.New("internal error")
		},
	})

	_, err := h.AddMember(context.Background(), &pb.AddMemberRequest{
		WorkspaceId: uuid.New().String(),
		PersonId:    uuid.New().String(),
		Role:        "Member",
	})

	assert.Equal(t, codes.Internal, status.Code(err))
}

// ── UpdateMemberAccess ────────────────────────────────────────────────────────

func TestUpdateMemberAccess_Valid_ReturnsMappedMember(t *testing.T) {
	wsID := uuid.New()
	pID := uuid.New()
	member := entities.ChatWorkspaceMember{
		ID:          uuid.New(),
		WorkspaceID: wsID,
		PersonID:    pID,
		Role:        "Member",
		HasAccess:   true,
		CreatedAt:   time.Now(),
	}

	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		UpdateMemberAccessFn: func(_ context.Context, _, _ uuid.UUID, _ bool) (entities.ChatWorkspaceMember, error) {
			return member, nil
		},
	})

	resp, err := h.UpdateMemberAccess(context.Background(), &pb.UpdateMemberAccessRequest{
		WorkspaceId: wsID.String(),
		PersonId:    pID.String(),
		HasAccess:   true,
	})

	require.NoError(t, err)
	assert.True(t, resp.HasAccess)
	assert.Equal(t, member.ID.String(), resp.Id)
}

// ── ListMembers ───────────────────────────────────────────────────────────────

func TestListMembers_Valid_ReturnsMemberList(t *testing.T) {
	wsID := uuid.New()
	members := []entities.ChatWorkspaceMember{
		{ID: uuid.New(), WorkspaceID: wsID, Role: "Member", CreatedAt: time.Now()},
		{ID: uuid.New(), WorkspaceID: wsID, Role: "Admin", HasAccess: true, CreatedAt: time.Now()},
	}

	h := handlers.NewWorkspaceHandler(&mockWorkspaceSvc{
		ListMembersFn: func(_ context.Context, _ uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
			return members, nil
		},
	})

	resp, err := h.ListMembers(context.Background(), &pb.ListMembersRequest{
		WorkspaceId: wsID.String(),
	})

	require.NoError(t, err)
	assert.Len(t, resp.Members, 2)
}
