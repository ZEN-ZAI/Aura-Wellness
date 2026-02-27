package main

import (
	"database/sql"
	"fmt"
	"log"

	"github.com/aura-wellness/chat-service/internal/config"
	"github.com/aura-wellness/chat-service/internal/handlers"
	"github.com/aura-wellness/chat-service/internal/middleware"
	"github.com/aura-wellness/chat-service/internal/repository"
	"github.com/gin-gonic/gin"
	"github.com/golang-migrate/migrate/v4"
	"github.com/golang-migrate/migrate/v4/database/postgres"
	_ "github.com/golang-migrate/migrate/v4/source/file"
	_ "github.com/lib/pq"
)

func main() {
	cfg := config.Load()

	// Run database migrations
	if err := runMigrations(cfg); err != nil {
		log.Fatalf("Migration failed: %v", err)
	}

	// Connect to DB with pgx pool for application use
	pool, err := repository.NewPool(cfg)
	if err != nil {
		log.Fatalf("Database connection failed: %v", err)
	}
	defer pool.Close()

	// Wire up repositories and handlers
	workspaceRepo := repository.NewWorkspaceRepo(pool)
	memberRepo := repository.NewMemberRepo(pool)
	workspaceHandler := handlers.NewWorkspaceHandler(workspaceRepo, memberRepo)

	// Set up Gin router
	r := gin.Default()
	r.Use(middleware.APIKeyAuth(cfg.InternalAPIKey))

	api := r.Group("/api")
	workspaceHandler.RegisterRoutes(api)

	addr := ":" + cfg.Port
	log.Printf("Chat service starting on %s", addr)
	if err := r.Run(addr); err != nil {
		log.Fatalf("Server failed: %v", err)
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

	m, err := migrate.NewWithDatabaseInstance("file://./internal/migrations", "postgres", driver)
	if err != nil {
		return fmt.Errorf("migrate instance: %w", err)
	}

	if err := m.Up(); err != nil && err != migrate.ErrNoChange {
		return fmt.Errorf("migrate up: %w", err)
	}

	log.Println("Migrations applied successfully")
	return nil
}
