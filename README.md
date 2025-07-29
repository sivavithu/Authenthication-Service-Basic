# Auth Service - Beginner-Friendly JWT Authentication Microservice

Welcome to the **Auth Service**! This is a beginner-friendly microservice built in ASP.NET Core for handling user authentication with JWT (JSON Web Tokens). It provides secure endpoints for registration, login, and token refresh, using password hashing and Entity Framework Core for database interactions. Ideal for developers learning microservices or building secure APIs, this service integrates seamlessly with an API gateway and other services like CRUD operations. It's designed to be easy, scalable, and easy to extend.

![Auth Service Architecture](https://via.placeholder.com/1200x400.png?text=Auth+Service+Demo)  
*(Add a screenshot or diagram showing the auth flow – replace with your own image URL for better visual appeal)*

## ✨ Why This Auth Service Stands Out
This project is crafted with beginners in mind, providing a straightforward implementation that demonstrates real-world auth practices without overwhelming complexity. Here's what makes it special:

- **Beginner-Friendly**: Clearendorf code structure, detailed comments, and step-by-step setup guide for those new to .NET and microservices. No advanced concepts are required to get started.
- **Secure Authentication**: Uses JWT for stateless tokens, password hashing with ASP.NET Identity (PBKDF2 algorithm), and refresh token rotation for added security against replay attacks.
- **Microservices-Ready**: Independent service with its own database, perfect for integration with API gateways (e.g., Ocelot) and other services like CRUD or notification systems.
- **Customizable and Extensible**: Supports role-based access (e.g., "User" or "Admin") and can be easily extended for features like multi-factor authentication (MFA), email verification, or OAuth providers.
- **Efficient and Scalable**: Asynchronous operations with EF Core for handling high concurrency; lightweight with no unnecessary dependencies.
- **Open Source and Community-Driven**: Fork, contribute, and adapt this service for your projects – contributions are welcome to make it even better!

This service is part of a larger microservices ecosystem but can stand alone as a simple auth API. If you're building a full app, pair it with a CRUD service and API gateway for a complete system. The code is modular, allowing you to learn one piece at a time while building a functional auth backend.

## 🚀 Features
The Auth Service provides a robust set of features focused on secure user management and token handling. Here's a detailed look:

- **User Registration**: Securely registers users with username, password hashing, default role assignment ("User"), and automatic GUID ID generation. Prevents duplicate usernames with DB checks.
- **Login**: Validates credentials against hashed passwords, issues JWT access token (1-day expiry) and refresh token (7-day expiry). Returns tokens in a structured DTO for easy client consumption.
- **Token Refresh**: Validates refresh token from the database, checks expiry, issues new access and refresh tokens, and rotates the refresh token for added security.
- **Role-Based Claims**: Includes user role in JWT claims for downstream authorization (e.g., in CRUD services).
- **Database Integration**: Uses EF Core with SQL Server for user data storage, including migration support. Easy to migrate to other DBs like PostgreSQL or Azure SQL.
- **Error Handling**: Returns meaningful HTTP status codes and messages (e.g., 400 for invalid credentials, 401 for unauthorized refresh requests) for better client-side error management.
- **Extensible Design**: Add features like email MFA, social logins (Google/Facebook), or user profile management with minimal changes to the service layer.

The service is stateless for token validation but stateful for refresh tokens (DB check) – a balanced approach for security and performance. It follows best practices from Microsoft documentation and OWASP guidelines for secure auth.

## 🛠️ Prerequisites
Before getting started, ensure you have the following tools and setup:

- **.NET SDK** (version 8 or later) installed – download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download). This is essential for building and running the service.
- **SQL Server** (Express edition is fine for development) – install from Microsoft or use a cloud instance like Azure SQL. The service uses EF Core, so other relational DBs can be swapped in.
- **Visual Studio 2022** or VS Code with C# extension for editing, debugging, and running migrations.
- **Postman** or a similar API testing tool (e.g., curl) to test endpoints like login and refresh.
- Optional: Docker for containerization if deploying to cloud environments like Azure or AWS.

## 📋 Installation and Setup
Follow these steps to get the Auth Service up and running locally. The process is straightforward and takes just a few minutes.

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/auth-service.git
   cd auth-service
   ```

2. **Restore Dependencies**  
   Restore NuGet packages to download required libraries:  
   ```bash
   dotnet restore
   ```

3. **Configure the Database**  
   Update `appsettings.json` with your SQL Server connection string (replace with your server details):  
   ```json
   "ConnectionStrings": {
     "UserDatabase": "Server = your-server-name; Database = UserDb; Trusted_Connection = true; TrustServerCertificate = true;"
   }
   ```  
   For JWT settings (issuer, audience, key – generate a strong key for production and keep it secret):  
   ```json
   "AppSettings": {
     "Issuer": "loginapp",
     "Audience": "myAwesomeAudience",
     "Key": "your-long-secure-key-here"
   }
   ```  
   Note: For production, use environment variables or a secrets manager (e.g., Azure Key Vault) to store sensitive data like the JWT key.

4. **Apply Database Migrations**  
   Add the initial migration to create the schema:  
   ```bash
   dotnet ef migrations add InitialCreate
   ```  
   Apply the migration to update the database:  
   ```bash
   dotnet ef database update
   ```  
   This creates the "UserDb" database and the "Users" table with columns for Id, Username, PasswordHash, Role, RefreshToken, and RefreshTokenExpiryTime. If using a different DB, update the provider in `Program.cs`.

5. **Run the Service**  
   Start the app in the console:  
   ```bash
   dotnet run
   ```  
   Or in Visual Studio: Press F5 to debug or Ctrl+F5 to run without debugging.  
   The service will start listening on http://localhost:5001 (or HTTPS if configured) – check the console for confirmation messages like "Application started. Press Ctrl+C to shut down."

6. **Test the Endpoints**  
   Use Postman or curl to verify the service is working:  
   - Register User: POST http://localhost:5001/register with JSON body `{"username": "testuser", "password": "testpass123"}` – expect 200 OK with "User registered successfully" (or 400 if the username already exists).  
   - Login: POST http://localhost:5001/login with the same body – expect 200 OK with `{"accessToken": "jwt-string", "refreshToken": "refresh-string"}`.  
   - Refresh Token: POST http://localhost:5001/refresh-token with `{"userId": "guid-from-jwt", "refreshToken": "from-login"}` – expect new tokens (or 401 if invalid).  
   If errors occur (e.g., DB connection fail), show messages in the console logs for details and verify the connection string.

## 📂 Project Structure
The project is organized into logical folders for easy navigation and understanding. Here's a breakdown of the key files and directories:

- `AuthService.csproj`: The main project file defining dependencies and build settings.
- `Program.cs`: The entry point of the application, where services (DbContext, AuthService, JWT authentication) are registered and the app pipeline (authentication, authorization, controllers) is configured.
- `appsettings.json`: Configuration file for logging, allowed hosts, JWT settings (issuer, audience, key), and database connection string.
- `Entities/`: Contains domain models.  
  - `User.cs`: The User entity with properties like Id (Guid), Username, PasswordHash, Role, RefreshToken, and RefreshTokenExpiryTime.
- `Data/`: Database-related code.  
  - `ApplicationDbContext.cs`: EF Core DbContext with DbSet<User> for the Users table.
- `Models/`: Data Transfer Objects (DTOs) for API input/output.  
  - `UserDto.cs`: For username and password in register/login requests.  
  - `TokenResponseDto.cs`: For accessToken and refreshToken in login/refresh responses.  
  - `RefreshTokenRequestDto.cs`: For userId and refreshToken in refresh requests.
- `Service/`: Business logic layer.  
  - `IAuthService.cs`: Interface for auth methods (LoginAsync, RegisterAsync, RefreshTokenAsync).  
  - `AuthService.cs`: Implementation with password verification, token generation, refresh validation, and DB operations.
- `Controllers/`: API layer.  
  - `AuthController.cs`: Endpoints for POST register, login, and refresh-token, with dependency injection for IAuthService.
- `Migrations/`: EF Core migration files (generated on dotnet ef commands) for database schema changes.

This structure follows clean architecture principles, separating concerns for better maintainability.

```
Authenthication-Service-Basic/
├── AuthService/                 # Main service project directory
│   ├── Controllers/             # API Controllers (e.g., AuthController.cs)
│   ├── Models/                  # User models, request/response DTOs
│   ├── Services/                # (If present) Auth-related business logic
│   ├── Program.cs               # Entry point, service configuration
│   ├── appsettings.json         # Basic application config
│   └── ...                      # Other .NET project files
├── Dockerfile                   # Containerization support
├── README.md                    # Project info (you're here!)
├── .gitignore                   
└── AuthService.sln              # Visual Studio solution file
```

## 🔧 Configuration Details
JWT Setup: In `Program.cs`, `AddAuthentication` configures token validation parameters (issuer, audience, key) – essential for secure token issuance and validation.  
Password Hashing: Uses ASP.NET Identity's `PasswordHasher` for secure storage (PBKDF2 algorithm with salting).  
Refresh Tokens: Stored in the DB with expiry timestamps and rotated on each use to enhance security.  
Customization: Extend for advanced features like multi-factor authentication (MFA) by adding OTP logic to the service, or role-based enhancements by modifying claims in `CreateToken`.  
Logging: Basic console logging; add Serilog for more advanced file/structured logging if needed.

## 🤝 Contributing
This project is beginner-friendly and open for contributions! If you have ideas for improvements, such as adding MFA or supporting additional auth providers, fork the repo and submit a pull request. Issues and suggestions are welcome to help make this even better for the community.

## 📄 License
MIT License – Free to use, modify, and distribute. See the LICENSE file for details.

## 🌟 Star This Repo
If this helps you learn or build, please star it on GitHub! Explore the companion API Gateway and CRUD Service repos for a full microservices stack.
