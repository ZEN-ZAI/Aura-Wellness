package config

import "os"

type Config struct {
	DBHost         string
	DBPort         string
	DBName         string
	DBUser         string
	DBPassword     string
	InternalAPIKey string
	Port           string
	WsPort         string
	RedisAddr      string
	JWTSecret      string
}

func Load() *Config {
	return &Config{
		DBHost:         getEnv("DB_HOST", "localhost"),
		DBPort:         getEnv("DB_PORT", "5432"),
		DBName:         getEnv("DB_NAME", "aura_chat"),
		DBUser:         getEnv("DB_USER", "chat_user"),
		DBPassword:     getEnv("DB_PASSWORD", "changeme_chat"),
		InternalAPIKey: getEnv("INTERNAL_API_KEY", "dev_internal_key"),
		Port:           getEnv("GRPC_PORT", getEnv("PORT", "50051")),
		WsPort:         getEnv("WS_PORT", "8080"),
		RedisAddr:      getEnv("REDIS_ADDR", "localhost:6379"),
		JWTSecret:      getEnv("JWT_SECRET", ""),
	}
}

func getEnv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
