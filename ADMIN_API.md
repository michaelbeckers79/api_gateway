# Admin API Documentation

This document describes the administrative API endpoints for managing users, routes, clusters, and sessions.

## Authentication

All admin endpoints require **Client Credentials** authentication using HTTP Basic Auth.

```bash
# Format
Authorization: Basic base64(clientId:clientSecret)

# Example
curl -u admin-client:your-secret http://localhost:5261/admin/routes
```

## User Management

### List All Users

```http
GET /admin/users
```

**Response:**
```json
[
  {
    "id": 1,
    "username": "john.doe",
    "email": "john@example.com",
    "isEnabled": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-05T10:30:00Z",
    "activeSessions": 2
  }
]
```

### Get User Details

```http
GET /admin/users/{id}
```

**Response:**
```json
{
  "id": 1,
  "username": "john.doe",
  "email": "john@example.com",
  "isEnabled": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-05T10:30:00Z",
  "sessions": [
    {
      "id": 1,
      "tokenId": "abc123...",
      "createdAt": "2024-01-05T10:00:00Z",
      "lastAccessedAt": "2024-01-05T10:30:00Z",
      "expiresAt": "2024-01-05T18:00:00Z",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "isExpired": false
    }
  ]
}
```

### Enable User

```http
POST /admin/users/{id}/enable
```

**Response:**
```json
{
  "message": "User enabled"
}
```

### Disable User

```http
POST /admin/users/{id}/disable
```

**Response:**
```json
{
  "message": "User disabled"
}
```

## Passkey Management

### Register User Passkey

Register a passkey for a user. The passkey is stored as plain text in the database for demonstration purposes. In production, this should be hashed.

```http
POST /admin/users/{userId}/passkey
```

**Request:**
```json
{
  "passkey": "my-secure-passkey-12345"
}
```

**Response:**
```json
{
  "message": "Passkey registered successfully"
}
```

### Get Passkey Status

Check if a user has a registered passkey.

```http
GET /admin/users/{userId}/passkey
```

**Response:**
```json
{
  "userId": 1,
  "username": "john.doe",
  "hasPasskey": true
}
```

### Validate Passkey

Validate a passkey for a user.

```http
POST /admin/users/{userId}/passkey/validate
```

**Request:**
```json
{
  "passkey": "my-secure-passkey-12345"
}
```

**Response:**
```json
{
  "isValid": true
}
```

## Session Management

### Get User Sessions

```http
GET /admin/users/{userId}/sessions
```

**Response:**
```json
[
  {
    "id": 1,
    "tokenId": "abc123...",
    "createdAt": "2024-01-05T10:00:00Z",
    "lastAccessedAt": "2024-01-05T10:30:00Z",
    "expiresAt": "2024-01-05T18:00:00Z",
    "ipAddress": "192.168.1.100",
    "userAgent": "Mozilla/5.0...",
    "isRevoked": false,
    "isExpired": false
  }
]
```

### Revoke Single Session

```http
POST /admin/sessions/{sessionId}/revoke
```

**Response:**
```json
{
  "message": "Session revoked"
}
```

### Revoke All User Sessions

```http
POST /admin/users/{userId}/sessions/revoke-all
```

**Response:**
```json
{
  "message": "Revoked 3 sessions"
}
```

## Route Management

### List All Routes

```http
GET /admin/routes
```

**Response:**
```json
[
  {
    "id": 1,
    "routeId": "api-route",
    "clusterId": "backend-api",
    "match": "/api/{**catch-all}",
    "order": 1,
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

### Create Route

```http
POST /admin/routes
Content-Type: application/json

{
  "routeId": "new-route",
  "clusterId": "backend-api",
  "match": "/myapi/{**catch-all}",
  "order": 2,
  "isActive": true
}
```

**Response:**
```json
{
  "id": 2,
  "routeId": "new-route",
  "clusterId": "backend-api",
  "match": "/myapi/{**catch-all}",
  "order": 2,
  "isActive": true,
  "createdAt": "2024-01-05T10:00:00Z",
  "updatedAt": "2024-01-05T10:00:00Z"
}
```

### Update Route

```http
PUT /admin/routes/{id}
Content-Type: application/json

{
  "routeId": "updated-route",
  "clusterId": "backend-api",
  "match": "/newapi/{**catch-all}",
  "order": 1,
  "isActive": true
}
```

### Delete Route

```http
DELETE /admin/routes/{id}
```

**Response:**
```json
{
  "message": "Route deleted"
}
```

## Cluster Management

### List All Clusters

```http
GET /admin/clusters
```

**Response:**
```json
[
  {
    "id": 1,
    "clusterId": "backend-api",
    "destinationAddress": "http://localhost:5001",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

### Create Cluster

```http
POST /admin/clusters
Content-Type: application/json

{
  "clusterId": "new-service",
  "destinationAddress": "http://localhost:5002",
  "isActive": true
}
```

**Response:**
```json
{
  "id": 2,
  "clusterId": "new-service",
  "destinationAddress": "http://localhost:5002",
  "isActive": true,
  "createdAt": "2024-01-05T10:00:00Z",
  "updatedAt": "2024-01-05T10:00:00Z"
}
```

### Update Cluster

```http
PUT /admin/clusters/{id}
Content-Type: application/json

{
  "clusterId": "updated-service",
  "destinationAddress": "http://localhost:5003",
  "isActive": true
}
```

### Delete Cluster

```http
DELETE /admin/clusters/{id}
```

**Response:**
```json
{
  "message": "Cluster deleted"
}
```

## Client Credentials Management

### List All Clients

```http
GET /admin/clients
```

**Response:**
```json
[
  {
    "id": 1,
    "clientId": "admin-client",
    "description": "Admin API client",
    "isEnabled": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUsedAt": "2024-01-05T10:30:00Z"
  }
]
```

### Create Client

```http
POST /admin/clients
Content-Type: application/json

{
  "clientId": "new-client",
  "clientSecret": "secure-secret-123",
  "description": "New API client"
}
```

**Response:**
```json
{
  "id": 2,
  "clientId": "new-client",
  "description": "New API client",
  "isEnabled": true,
  "createdAt": "2024-01-05T10:00:00Z"
}
```

**Note:** The client secret is hashed and never returned. Store it securely when creating a client.

### Enable Client

```http
POST /admin/clients/{id}/enable
```

**Response:**
```json
{
  "message": "Client enabled"
}
```

### Disable Client

```http
POST /admin/clients/{id}/disable
```

**Response:**
```json
{
  "message": "Client disabled"
}
```

### Delete Client

```http
DELETE /admin/clients/{id}
```

**Response:**
```json
{
  "message": "Client deleted"
}
```

## Configuration Reload

When routes or clusters are created, updated, or deleted, the YARP configuration is automatically reloaded without requiring a server restart.

## Error Responses

### Unauthorized (401)

```json
{
  "error": "invalid_client"
}
```

### Not Found (404)

No response body (standard 404 status).

### Bad Request (400)

```json
{
  "error": "Client with ID new-client already exists"
}
```

## Usage Examples

### Create an Admin Client

First, you need to create an admin client in the database:

```bash
# Calculate the hash for the secret
echo -n "your-secret" | sha256sum | xxd -r -p | base64

# Insert into database
sqlite3 apigateway.db "INSERT INTO ClientCredentials (ClientId, ClientSecretHash, Description, IsEnabled, CreatedAt) VALUES ('admin-client', '<hash>', 'Admin API client', 1, datetime('now'));"
```

### Disable a User

```bash
curl -X POST -u admin-client:your-secret \
  http://localhost:5261/admin/users/1/disable
```

### Revoke All Sessions for a User

```bash
curl -X POST -u admin-client:your-secret \
  http://localhost:5261/admin/users/1/sessions/revoke-all
```

### Add a New Route

```bash
curl -X POST -u admin-client:your-secret \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "users-api",
    "clusterId": "backend-api",
    "match": "/users/{**catch-all}",
    "order": 1,
    "isActive": true
  }' \
  http://localhost:5261/admin/routes
```

## Security Considerations

1. **Client Secrets**: Always use strong, randomly generated client secrets
2. **HTTPS**: Only use admin API over HTTPS in production
3. **Secret Storage**: Never commit client secrets to source control
4. **Rotation**: Regularly rotate client credentials
5. **Audit Logging**: Monitor admin API usage
6. **Network Security**: Restrict admin API access to trusted networks
7. **Rate Limiting**: Consider implementing rate limiting for admin endpoints

## JWT Configuration

The gateway extracts the username from JWT tokens using a configurable claim name. Update `appsettings.json`:

```json
{
  "Jwt": {
    "UsernameClaim": "preferred_username"
  }
}
```

Common claim names:
- `preferred_username` (Azure AD, Keycloak)
- `username` (Auth0)
- `email` (Google, many providers)
- `upn` (Active Directory)
- `sub` (Subject identifier)
- `name` (Display name)

The service automatically falls back to common claim names if the configured claim is not found.
