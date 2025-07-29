# Auth Service - Beginner-Friendly JWT Authentication Microservice

Welcome to the **Auth Service**! This is a beginner-friendly microservice built in ASP.NET Core for handling user authentication with JWT (JSON Web Tokens). It provides secure endpoints for registration, login, and token refresh, using password hashing and Entity Framework Core for database interactions. Ideal for developers learning microservices or building secure APIs, this service integrates seamlessly with an API gateway and other services like CRUD operations. It's designed to be easy, scalable, and extensible.

## 🚀 Features
The Auth Service provides a robust set of features focused on secure user management and token handling:

- **User Registration**: Securely registers users with username, password hashing, default role assignment ("User"), and automatic GUID ID generation. Prevents duplicate usernames with DB checks.
- **Login**: Validates credentials against hashed passwords, issues JWT access token (1-day expiry) and refresh token (7-day expiry). Returns tokens in a structured DTO for easy client consumption.
- **Token Refresh**: Validates refresh token from the database, checks expiry, issues new access and refresh tokens, and rotates the refresh token for added security.
- **Database Integration**: Uses EF Core with SQL Server for user data storage, including migration support. Easily migrates to other DBs like PostgreSQL or Azure SQL.
- **Error Handling**: Returns meaningful HTTP status codes and messages (e.g., 400 for invalid credentials, 401 for unauthorized refresh requests) for better client-side error management.

## 🛠️ Prerequisites
Before getting started, ensure you have the following:

- **.NET SDK** (version 8 or later) – download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).
- **SQL Server** (Express edition works for development) – install from Microsoft or use a cloud instance like Azure SQL. EF Core supports other relational DBs.
- **Visual Studio 2022** or VS Code with C# extension for editing, debugging, and running migrations.
- **Postman** or a similar API testing tool (e.g., curl) to test endpoints.
- Optional: Docker for containerization if deploying to cloud environments like Azure or AWS.

## 📋 Installation and Setup
Follow these steps to get the Auth Service running locally:

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
   Update `appsettings.json` with your SQL Server connection string:  
   ```json
   "ConnectionStrings": {
     "UserDatabase": "Server=your-server-name;Database=UserDb;Trusted_Connection=true;TrustServerCertificate=true;"
   }
   ```  
   For JWT settings (generate a strong key for production and keep it secret):  
   ```json
   "AppSettings": {
     "Issuer": "loginapp",
     "Audience": "myAwesomeAudience",
     "Key": "your-long-secure-key-here"
   }
   ```  
   *Note*: For production, use environment variables or a secrets manager (e.g., Azure Key Vault) for sensitive data.

4. **Apply Database Migrations**  
   Add the initial migration to create the schema:  
   ```bash
   dotnet ef migrations add InitialCreate
   ```  
   Apply the migration to update the database:  
   ```bash
   dotnet ef database update
   ```  
   This creates the "UserDb" database and "Users" table with columns for Id, Username, PasswordHash, Role, RefreshToken, and RefreshTokenExpiryTime. For other DBs, update the provider in `Program.cs`.

5. **Run the Service**  
   Start the app in the console:  
   ```bash
   dotnet run
   ```  
   Or in Visual Studio: Press F5 to debug or Ctrl+F5 to run without debugging.  
   The service listens on `http://localhost:5001` (or HTTPS if configured) – check the console for "Application started. Press Ctrl+C to shut down."

6. **Test the Endpoints**  
   Use Postman or curl to verify functionality:  
   - **Register User**: POST `http://localhost:5001/register` with `{"username": "testuser", "password": "testpass123"}` – expect 200 OK with "User registered successfully" (or 400 if username exists).  
   - **Login**: POST `http://localhost:5001/login` with the same body – expect 200 OK with `{"accessToken": "jwt-string", "refreshToken": "refresh-string"}`.  
   - **Refresh Token**: POST `http://localhost:5001/refresh-token` with `{"userId": "guid-from-jwt", "refreshToken": "from-login"}` – expect new tokens (or 401 if invalid).  
   Check console logs for errors (e.g., DB connection issues) and verify the connection string.

## 📂 Project Structure
```plaintext
Authenthication-Service-Basic/
├── AuthService/                 # Main service project directory
│   ├── Controllers/             # API Controllers (e.g., AuthController.cs)
│   ├── Models/                  # User models, request/response DTOs
│   ├── Services/                # Auth-related business logic
│   ├── Program.cs               # Entry point, service configuration
│   ├── appsettings.json         # Application config
│   └── ...                      # Other .NET project files
├── Dockerfile                   # Containerization support
├── README.md                    # Project info (you're here!)
├── .gitignore                   # Ignored files
└── AuthService.sln              # Visual Studio solution file
```

## 🔧 Configuration Details
- **JWT Setup**: In `Program.cs`, `AddAuthentication` configures token validation parameters (issuer, audience, key) for secure token issuance and validation.
- **Password Hashing**: Uses ASP.NET Identity's `PasswordHasher` with PBKDF2 algorithm and salting for secure storage.
- **Refresh Tokens**: Stored in the DB with expiry timestamps and rotated on each use for enhanced security.

## 🤝 Contributing
This project is beginner-friendly and open for contributions! To add features like MFA or additional auth providers, fork the repo and submit a pull request. Issues and suggestions are welcome to improve the community experience.

## 📄 License
MIT License – Free to use, modify, and distribute. See the LICENSE file for details.

## 🌟 Star This Repo
If this helps you learn or build, please star it on GitHub! Check out the companion API Gateway and CRUD Service repos for a full microservices stack.
