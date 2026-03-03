package application_test

import (
	"context"
	"errors"
	"testing"

	"github.com/aura-wellness/chat-service/internal/application"
	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/mocks"
	"github.com/google/uuid"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

// ── CreateWorkspace ───────────────────────────────────────────────────────────

func TestCreateWorkspace_Valid_DelegatesToRepoAndReturnsWorkspace(t *testing.T) {
	buID := uuid.New()
	companyID := uuid.New()
	name := "Test Workspace"
	created := entities.ChatWorkspace{ID: uuid.New(), BuID: buID, CompanyID: companyID, Name: name}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{
			CreateFn: func(_ context.Context, w entities.ChatWorkspace) (entities.ChatWorkspace, error) {
				assert.Equal(t, buID, w.BuID)
				assert.Equal(t, name, w.Name)
				return created, nil
			},
		},
		&mocks.MockMemberRepository{},
	)

	got, err := svc.CreateWorkspace(context.Background(), buID, companyID, name)

	require.NoError(t, err)
	assert.Equal(t, created.ID, got.ID)
}

func TestCreateWorkspace_EmptyName_ReturnsValidationError(t *testing.T) {
	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{},
	)

	_, err := svc.CreateWorkspace(context.Background(), uuid.New(), uuid.New(), "")

	assert.Error(t, err)
}

// ── GetByBuID ─────────────────────────────────────────────────────────────────

func TestGetByBuID_DelegatesToRepo(t *testing.T) {
	buID := uuid.New()
	expected := entities.ChatWorkspace{ID: uuid.New(), BuID: buID}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{
			GetByBuIDFn: func(_ context.Context, id uuid.UUID) (entities.ChatWorkspace, error) {
				assert.Equal(t, buID, id)
				return expected, nil
			},
		},
		&mocks.MockMemberRepository{},
	)

	got, err := svc.GetByBuID(context.Background(), buID)

	require.NoError(t, err)
	assert.Equal(t, expected.ID, got.ID)
}

// ── GetByID ───────────────────────────────────────────────────────────────────

func TestGetByID_DelegatesToRepo(t *testing.T) {
	id := uuid.New()
	expected := entities.ChatWorkspace{ID: id}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{
			GetByIDFn: func(_ context.Context, wsID uuid.UUID) (entities.ChatWorkspace, error) {
				assert.Equal(t, id, wsID)
				return expected, nil
			},
		},
		&mocks.MockMemberRepository{},
	)

	got, err := svc.GetByID(context.Background(), id)

	require.NoError(t, err)
	assert.Equal(t, expected.ID, got.ID)
}

// ── AddMember ─────────────────────────────────────────────────────────────────

func TestAddMember_EmptyRole_ReturnsValidationError(t *testing.T) {
	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{},
	)

	_, err := svc.AddMember(context.Background(), uuid.New(), uuid.New(), "")

	assert.Error(t, err)
}

func TestAddMember_Valid_CallsUpsertAndReturnsMember(t *testing.T) {
	wsID := uuid.New()
	pID := uuid.New()
	expected := entities.ChatWorkspaceMember{ID: uuid.New(), WorkspaceID: wsID, PersonID: pID, Role: "Member"}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{
			UpsertFn: func(_ context.Context, m entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error) {
				assert.Equal(t, wsID, m.WorkspaceID)
				assert.Equal(t, pID, m.PersonID)
				assert.Equal(t, "Member", m.Role)
				return expected, nil
			},
		},
	)

	got, err := svc.AddMember(context.Background(), wsID, pID, "Member")

	require.NoError(t, err)
	assert.Equal(t, expected.ID, got.ID)
}

func TestAddMember_AdminRole_HasAccessByDefault(t *testing.T) {
	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{
			UpsertFn: func(_ context.Context, m entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error) {
				assert.True(t, m.HasAccess, "Admin members should have access by default")
				return m, nil
			},
		},
	)

	_, err := svc.AddMember(context.Background(), uuid.New(), uuid.New(), "Admin")
	require.NoError(t, err)
}

// ── UpdateMemberAccess ────────────────────────────────────────────────────────

func TestUpdateMemberAccess_DelegatesToRepo(t *testing.T) {
	wsID := uuid.New()
	pID := uuid.New()
	expected := entities.ChatWorkspaceMember{WorkspaceID: wsID, PersonID: pID, HasAccess: true}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{
			UpdateAccessFn: func(_ context.Context, w, p uuid.UUID, access bool) (entities.ChatWorkspaceMember, error) {
				assert.Equal(t, wsID, w)
				assert.Equal(t, pID, p)
				assert.True(t, access)
				return expected, nil
			},
		},
	)

	got, err := svc.UpdateMemberAccess(context.Background(), wsID, pID, true)

	require.NoError(t, err)
	assert.True(t, got.HasAccess)
}

// ── ListMembers ───────────────────────────────────────────────────────────────

func TestListMembers_DelegatesToRepo(t *testing.T) {
	wsID := uuid.New()
	expected := []entities.ChatWorkspaceMember{
		{ID: uuid.New(), WorkspaceID: wsID},
		{ID: uuid.New(), WorkspaceID: wsID},
	}

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{
			ListByWorkspaceFn: func(_ context.Context, id uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
				assert.Equal(t, wsID, id)
				return expected, nil
			},
		},
	)

	got, err := svc.ListMembers(context.Background(), wsID)

	require.NoError(t, err)
	assert.Len(t, got, 2)
}

func TestListMembers_RepoError_ReturnsError(t *testing.T) {
	repoErr := errors.New("db error")

	svc := application.NewWorkspaceService(
		&mocks.MockWorkspaceRepository{},
		&mocks.MockMemberRepository{
			ListByWorkspaceFn: func(_ context.Context, _ uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
				return nil, repoErr
			},
		},
	)

	_, err := svc.ListMembers(context.Background(), uuid.New())

	assert.ErrorIs(t, err, repoErr)
}
