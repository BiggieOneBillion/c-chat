# Real-time Communication (SignalR)

This document describes the real-time communication protocols used by the `c-chat` backend via SignalR.

## Hub Connection
- **Endpoint**: `/chathub`
- **Authentication**: Requires a JWT token passed in the query string as `access_token`.

## Client Invokable Methods (Client -> Server)

### SendMessage
Sends a message to a specific chat group.
- **Arguments**: `chatId` (string), `messageContent` (string)

### JoinChat
Joins a specific chat group to start receiving live updates.
- **Arguments**: `chatId` (string)

### LeaveChat
Leaves a chat group.
- **Arguments**: `chatId` (string)

### SetTyping
Indicates that the current user is typing.
- **Arguments**: `chatId` (string), `isTyping` (boolean)

---

## Hub Events (Server -> Client)

### ReceiveMessage
Broadcasted when a new message is sent to a chat.
- **Data**: `senderUsername`, `content`, `timestamp`

### UserTyping
Notifies clients when a user starts or stops typing.
- **Data**: `username`, `isTyping`

### UserOnlineStatus
Broadcasted when a user connects or disconnects.
- **Data**: `username`, `isOnline`

### UserJoinedChat / UserLeftChat
Notifies participants when someone joins or leaves.
- **Data**: `username`, `chatId`

### InvitationReceived
Sent to a specific user when they are invited to a chat.
- **Data**: `invitationData` (object)
