package main

import (
	"database/sql"
	"fmt"
	"log"
	"net"

	"github.com/golang-migrate/migrate/v4"
	"github.com/golang-migrate/migrate/v4/database/postgres"
	_ "github.com/golang-migrate/migrate/v4/source/file"
	_ "github.com/lib/pq"

	"github.com/aura-wellness/chat-service/internal/application"
	"github.com/aura-wellness/chat-service/internal/config"
	infraPostgres "github.com/aura-wellness/chat-service/internal/infrastructure/postgres"
	infraRedis "github.com/aura-wellness/chat-service/internal/infrastructure/redis"
	transportGrpc "github.com/aura-wellness/chat-service/internal/transport/grpc"
)

func main() {
	cfg := config.Load()

	if err := runMigrations(cfg); err != nil {
		log.Fatalf("Migration failed: %v", err)
	}

	// Infrastructure layer — concrete adapters wired up first
	pool, err := infraPostgres.NewPool(cfg)
	if err != nil {
		log.Fatalf("Database connection failed: %v", err)
	}
	defer pool.Close()

	pubsub := infraRedis.NewPubSub(cfg.RedisAddr)

	workspaceRepo := infraPostgres.NewWorkspaceRepo(pool)
	memberRepo := infraPostgres.NewMemberRepo(pool)
	messageRepo := infraPostgres.NewMessageRepo(pool)

	// Application layer — services depend only on port interfaces
	workspaceSvc := application.NewWorkspaceService(workspaceRepo, memberRepo)
	messagingSvc := application.NewMessagingService(messageRepo, memberRepo, pubsub)

	// Transport layer — gRPC server with interceptors + handlers
	srv := transportGrpc.NewServer(workspaceSvc, messagingSvc, cfg.InternalAPIKey)

	lis, err := net.Listen("tcp", ":"+cfg.Port)
	if err != nil {
		log.Fatalf("Failed to listen on :%s: %v", cfg.Port, err)
	}
	log.Printf("gRPC chat service listening on :%s", cfg.Port)
	if err := srv.Serve(lis); err != nil {
		log.Fatalf("gRPC serve error: %v", err)
	}
}

func runMigrations(cfg *config.Config) error {
	dsn := fmt.Sprintf(
		"postgres://%s:%s@%s:%s/%s?sslmode=disable",
		cfg.DBUser, cfg.DBPassword, cfg.DBHost, cfg.DBPort, cfg.DBName,
	)

	db, err := sql.Open("postgres", dsn)
	if err != nil {
		return fmt.Errorf("open db for migration: %w", err)
	}
	defer db.Close()

	driver, err := postgres.WithInstance(db, &postgres.Config{})
	if err != nil {
		return fmt.Errorf("migration driver: %w", err)
	}

	m, err := migrate.NewWithDatabaseInstance(
		"file://./internal/infrastructure/postgres/migrations",
		"postgres", driver,
	)
	if err != nil {
		return fmt.Errorf("migrate instance: %w", err)
	}

	if err := m.Up(); err != nil && err != migrate.ErrNoChange {
		return fmt.Errorf("migrate up: %w", err)
	}

	log.Println("Migrations applied successfully")
	return nil
}
