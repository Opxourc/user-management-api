# User Management API

A ASP.NET Core API for managing users. It stores data in memory while the app is running, which makes it easy to learn CRUD operations, request handling, validation, and authentication without needing a database.

## Features

- Create, read, update, and delete users
- In-memory data storage for quick testing
- Bearer token authentication
- Input validation for names and email addresses
- Request/response logging for debugging
- Simple JSON-based API responses

## Project Structure

- Source/Program.cs - app setup, middleware, and endpoints
- Source/Models/User.cs - user and request models
- Source/Services/UserStore.cs - in-memory storage and business logic
- Source/appsettings.json - app configuration, including the auth token

## Prerequisites

- .NET 10 SDK
- A terminal or command prompt

## Run the Project

1. Open a terminal in the project folder.
2. Move into the app folder:

```bash
cd Source
```

1. Start the API:

```bash
dotnet restore
dotnet run
```

The app runs locally with the configured URLs from the launch settings, typically:

- <http://localhost:5253>
- <https://localhost:7229>

## Authentication

Every request must include a Bearer token in the Authorization header.

```http
Authorization: Bearer demo-token
```

The default token is defined in:

- Source/appsettings.json

If you change it there, use the new value in your requests.

## API Endpoints

### Get all users

```http
GET /users
```

### Get one user by ID

```http
GET /users/{id}
```

### Create a user

```http
POST /users
```

Example body:

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane@example.com"
}
```

### Update a user

```http
PUT /users/{id}
```

Example body:

```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane.smith@example.com"
}
```

### Delete a user

```http
DELETE /users/{id}
```

## Example Request

```bash
curl -X GET "http://localhost:5253/users" \
  -H "Authorization: Bearer demo-token"
```

## Notes

- Data is not saved to a database and resets when the app stops.
- This is a good starting project for learning ASP.NET Core, API routes, validation, and auth middleware.
