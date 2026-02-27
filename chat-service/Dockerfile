FROM golang:1.25-alpine AS builder
WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o chat-service ./main.go

FROM alpine:3.21
RUN apk add --no-cache ca-certificates
WORKDIR /app
COPY --from=builder /app/chat-service .
COPY --from=builder /app/internal/migrations ./internal/migrations
EXPOSE 8080
ENTRYPOINT ["./chat-service"]
