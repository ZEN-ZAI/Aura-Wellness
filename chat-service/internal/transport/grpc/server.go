package grpc

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/aura-wellness/chat-service/internal/transport/grpc/handlers"
	"github.com/aura-wellness/chat-service/internal/transport/grpc/interceptors"
	"google.golang.org/grpc"
)

// chatServiceServer implements pb.ChatServiceServer by delegating to the two
// focused handler structs. Explicit forwarding avoids Go embedding ambiguity
// when pb.UnimplementedChatServiceServer also provides the same method names.
type chatServiceServer struct {
	pb.UnimplementedChatServiceServer
	workspace *handlers.WorkspaceHandler
	messaging *handlers.MessagingHandler
}

// Workspace RPCs

func (s *chatServiceServer) CreateWorkspace(ctx context.Context, req *pb.CreateWorkspaceRequest) (*pb.Workspace, error) {
	return s.workspace.CreateWorkspace(ctx, req)
}

func (s *chatServiceServer) GetWorkspaceByBuId(ctx context.Context, req *pb.GetWorkspaceByBuIdRequest) (*pb.Workspace, error) {
	return s.workspace.GetWorkspaceByBuId(ctx, req)
}

func (s *chatServiceServer) GetWorkspaceById(ctx context.Context, req *pb.GetWorkspaceByIdRequest) (*pb.Workspace, error) {
	return s.workspace.GetWorkspaceById(ctx, req)
}

func (s *chatServiceServer) AddMember(ctx context.Context, req *pb.AddMemberRequest) (*pb.WorkspaceMember, error) {
	return s.workspace.AddMember(ctx, req)
}

func (s *chatServiceServer) UpdateMemberAccess(ctx context.Context, req *pb.UpdateMemberAccessRequest) (*pb.WorkspaceMember, error) {
	return s.workspace.UpdateMemberAccess(ctx, req)
}

func (s *chatServiceServer) ListMembers(ctx context.Context, req *pb.ListMembersRequest) (*pb.ListMembersResponse, error) {
	return s.workspace.ListMembers(ctx, req)
}

// Messaging RPCs

func (s *chatServiceServer) SendMessage(ctx context.Context, req *pb.SendMessageRequest) (*pb.ChatMessage, error) {
	return s.messaging.SendMessage(ctx, req)
}

func (s *chatServiceServer) ListMessages(ctx context.Context, req *pb.ListMessagesRequest) (*pb.ListMessagesResponse, error) {
	return s.messaging.ListMessages(ctx, req)
}

func (s *chatServiceServer) StreamMessages(req *pb.StreamMessagesRequest, stream pb.ChatService_StreamMessagesServer) error {
	return s.messaging.StreamMessages(req, stream)
}

// NewServer constructs a gRPC server with auth interceptors registered.
// All RPCs are automatically protected — no per-handler authentication needed.
func NewServer(workspaceSvc ports.WorkspaceService, messagingSvc ports.MessagingService, apiKey string) *grpc.Server {
	srv := grpc.NewServer(
		grpc.UnaryInterceptor(interceptors.UnaryAuthInterceptor(apiKey)),
		grpc.StreamInterceptor(interceptors.StreamAuthInterceptor(apiKey)),
	)

	pb.RegisterChatServiceServer(srv, &chatServiceServer{
		workspace: handlers.NewWorkspaceHandler(workspaceSvc),
		messaging: handlers.NewMessagingHandler(messagingSvc),
	})

	return srv
}
