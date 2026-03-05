package interceptors

import (
	"context"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/metadata"
	"google.golang.org/grpc/status"
)

func validate(ctx context.Context, apiKey string) error {
	md, ok := metadata.FromIncomingContext(ctx)
	if !ok {
		return status.Error(codes.Unauthenticated, "missing metadata")
	}
	vals := md.Get("x-internal-key")
	if len(vals) == 0 || vals[0] != apiKey {
		return status.Error(codes.Unauthenticated, "invalid api key")
	}
	return nil
}

// UnaryAuthInterceptor validates x-internal-key for all unary RPCs.
func UnaryAuthInterceptor(apiKey string) grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req any, _ *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (any, error) {
		if err := validate(ctx, apiKey); err != nil {
			return nil, err
		}
		return handler(ctx, req)
	}
}

// StreamAuthInterceptor validates x-internal-key for all streaming RPCs (e.g. StreamMessages).
func StreamAuthInterceptor(apiKey string) grpc.StreamServerInterceptor {
	return func(srv any, ss grpc.ServerStream, _ *grpc.StreamServerInfo, handler grpc.StreamHandler) error {
		if err := validate(ss.Context(), apiKey); err != nil {
			return err
		}
		return handler(srv, ss)
	}
}
