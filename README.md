# C-Chat | Terminal Chat App Backend

A robust, real-time chat backend built with **.NET 9** and **SignalR**, designed for terminal-based or standard web clients.

---

## 🚀 Key Features

- **Real-time Messaging**: Instant message delivery using SignalR WebSockets.
- **Secure Auth**: JWT-based authentication with password hashing via BCrypt.
- **Chat Management**: Support for private direct messages and group chats.
- **Invitations**: System for sending and managing chat membership invitations.
- **Flexible Storage**: Easy switching between In-Memory (Dev) and PostgreSQL (Prod).
- **Interactive API**: Built-in Swagger UI for exploring and testing endpoints.

## 🛠 Tech Stack

- **Framework**: .NET 9.0
- **Real-time**: ASP.NET Core SignalR
- **Database**: Entity Framework Core (PostgreSQL / In-Memory)
- **Security**: JWT Bearer Authentication, BCrypt.Net-Next
- **Documentation**: Swagger/OpenAPI, Markdown

## 📖 Documentation

Explore the detailed documentation to understand the project better:

- [🏗 **Architecture**](./docs/architecture.md) - Project structure and design patterns.
- [🔌 **API Reference**](./docs/api-reference.md) - RESTful endpoints and request/response models.
- [⚡ **Real-time Specs**](./docs/real-time.md) - SignalR hub methods and events list.
- [⚙️ **Setup Guide**](./docs/setup.md) - Instructions for local development and configuration.

## ⚡ Quick Start

```bash
# Clone and enter
git clone https://github.com/your-username/c-chat.git
cd c-chat/TerminalChatApp.Backend

# Restore and Run
dotnet restore
dotnet run
```

Access the API at `http://localhost:5000` and Swagger at `http://localhost:5000/swagger`.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ⚖️ License

This project is licensed under the MIT License.
