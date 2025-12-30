# API Reference

This document provides details about the RESTful API endpoints available in the `c-chat` backend.

## Base URL
The default development base URL is `http://localhost:5000/api`.

---

## Authentication

### Register User
`POST /auth/register`

Registers a new user account.

**Request Body:**
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securepassword123"
}
```

### User Login
`POST /auth/login`

Authenticates a user and returns a JWT token.

**Request Body:**
```json
{
  "username": "johndoe",
  "password": "securepassword123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5...",
  "username": "johndoe",
  "userId": "guid-string"
}
```

---

## Chats

### Get My Chats
`GET /chats`

Returns a list of all chats the authenticated user is participating in.

### Create Chat
`POST /chats`

Creates a new group or private chat.

**Request Body:**
```json
{
  "name": "Project Alpha",
  "type": "Group",
  "participantIds": ["guid-1", "guid-2"]
}
```

### Get Messages
`GET /chats/{chatId}/messages`

Retrieves message history for a specific chat.

---

## Invitations

### Send Invitation
`POST /invitations`

Invites a user to a specific chat.

### Handle Invitation
`POST /invitations/{invitationId}/respond`

Accept or decline a chat invitation.

---

## Health Check

### System Status
`GET /health`

Returns the current status of the API and its dependencies.
