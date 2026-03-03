package application

import "errors"

// ErrChatAccessDenied is returned when a member lacks chat access permission.
// The transport layer maps this to codes.PermissionDenied.
var ErrChatAccessDenied = errors.New("chat access denied")
