package ws

import (
	"errors"
	"net/http"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
)

// UserClaims holds the identity extracted from the frontend JWT.
type UserClaims struct {
	PersonID   uuid.UUID
	BuID       uuid.UUID
	SenderName string
}

var (
	errMissingToken  = errors.New("missing authorization token")
	errInvalidToken  = errors.New("invalid or expired token")
	errInvalidClaims = errors.New("token claims are malformed")
)

// ParseClaims validates the Bearer token from the Authorization header and
// returns typed UserClaims on success. Returns an HTTP-friendly status code
// alongside any error so the caller can respond with the right HTTP status.
func ParseClaims(r *http.Request, jwtSecret string) (*UserClaims, int, error) {
	authHeader := r.Header.Get("Authorization")
	if authHeader == "" {
		return nil, http.StatusUnauthorized, errMissingToken
	}
	tokenStr, ok := strings.CutPrefix(authHeader, "Bearer ")
	if !ok || tokenStr == "" {
		return nil, http.StatusUnauthorized, errMissingToken
	}

	token, err := jwt.Parse(tokenStr, func(t *jwt.Token) (interface{}, error) {
		if _, ok := t.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, errors.New("unexpected signing method")
		}
		return []byte(jwtSecret), nil
	}, jwt.WithIssuer("aura-wellness"), jwt.WithAudience("aura-wellness-client"))

	if err != nil || !token.Valid {
		return nil, http.StatusUnauthorized, errInvalidToken
	}

	mapClaims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		return nil, http.StatusUnauthorized, errInvalidClaims
	}

	personID, err := uuid.Parse(claimString(mapClaims, "personId"))
	if err != nil {
		return nil, http.StatusUnauthorized, errInvalidClaims
	}

	buID, err := uuid.Parse(claimString(mapClaims, "buId"))
	if err != nil {
		return nil, http.StatusUnauthorized, errInvalidClaims
	}

	firstName := claimString(mapClaims, "firstName")
	lastName := claimString(mapClaims, "lastName")
	senderName := strings.TrimSpace(firstName + " " + lastName)
	if senderName == "" {
		senderName = "Unknown"
	}

	return &UserClaims{
		PersonID:   personID,
		BuID:       buID,
		SenderName: senderName,
	}, http.StatusOK, nil
}

func claimString(c jwt.MapClaims, key string) string {
	if v, ok := c[key]; ok {
		if s, ok := v.(string); ok {
			return s
		}
	}
	return ""
}
