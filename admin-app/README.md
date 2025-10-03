# Admin Application

A Next.js-based admin panel for managing the API Gateway. This application provides a user-friendly interface to manage routes, clusters, users, clients, and test the OAuth token handler pattern.

## Features

- **Routes Management**: Create, edit, and delete API routes
- **Clusters Management**: Configure backend server clusters
- **Users Management**: View users, manage their status, and control sessions
- **Clients Management**: Manage API client credentials
- **Token Handler Testing**: Test OAuth authentication flows
- **Authentication**: Secure access with client credentials

## Prerequisites

- Node.js 18+ and npm
- Running API Gateway instance (default: http://localhost:5261)
- Valid client credentials for admin API access

## Getting Started

### 1. Navigate to the admin-app directory

```bash
cd admin-app
```

### 2. Install dependencies

```bash
npm install
```

### 3. Configure API base URL (optional)

Create a `.env.local` file in the `admin-app` directory:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5261
```

### 4. Run the development server

```bash
npm run dev
```

The application will be available at http://localhost:3000

### 5. Login

Use your admin client credentials to login:
- **Client ID**: Your admin client ID
- **Client Secret**: Your admin client secret

If you don't have credentials yet, see the main README for creating an admin client.

## Production Build

To create an optimized production build:

```bash
npm run build
npm start
```

## Technology Stack

- **Framework**: Next.js 15 (App Router)
- **UI Components**: shadcn/ui
- **Styling**: Tailwind CSS
- **Icons**: Lucide React
- **Language**: TypeScript

## Features Overview

### Dashboard
- Overview of routes, clusters, users, and active sessions
- Quick access to all management sections

### Routes Management
- List all configured routes
- Create new routes with route ID, cluster ID, match pattern, and order
- Edit existing routes
- Delete routes
- Toggle route active status

### Clusters Management
- List all backend clusters
- Create new clusters with cluster ID and destination address
- Edit existing clusters
- Delete clusters
- Toggle cluster active status

### Users Management
- View all registered users
- See user details including email, status, and last login
- View active sessions for each user
- Enable/disable user accounts
- Revoke individual sessions
- Revoke all sessions for a user

### Clients Management
- List all API client credentials
- Create new client credentials
- Enable/disable clients
- Delete clients
- View last usage information

### Token Handler Testing
- Test OAuth login flow initiation
- Check authentication status
- Test logout functionality
- View OAuth flow documentation

## Configuration

The application can be configured using environment variables:

- `NEXT_PUBLIC_API_BASE_URL`: Base URL for the API Gateway (default: http://localhost:5261)

## Security Notes

- Client credentials are stored in localStorage for convenience during development
- Use HTTPS in production
- Ensure proper CORS configuration in the API Gateway
- Regularly rotate admin client credentials
- Restrict admin panel access to trusted networks

## Development

### Project Structure

```
admin-app/
├── app/                    # Next.js App Router pages
│   ├── routes/            # Routes management page
│   ├── clusters/          # Clusters management page
│   ├── users/             # Users management page
│   ├── clients/           # Clients management page
│   ├── token-handler/     # Token handler testing page
│   ├── layout.tsx         # Root layout
│   ├── page.tsx           # Dashboard
│   └── globals.css        # Global styles
├── components/            # React components
│   ├── ui/               # shadcn/ui components
│   ├── auth-provider.tsx # Authentication context
│   ├── navigation.tsx    # Navigation menu
│   └── login-form.tsx    # Login form
├── lib/                  # Utilities
│   ├── api-client.ts     # API client
│   ├── config.ts         # Configuration
│   └── utils.ts          # Helper functions
└── package.json          # Dependencies
```

### Adding New Features

1. API client methods are in `lib/api-client.ts`
2. Create new pages in `app/[feature]/page.tsx`
3. Add navigation items in `components/navigation.tsx`
4. Use shadcn/ui components from `components/ui/`

## Troubleshooting

### CORS Errors

Ensure the API Gateway has CORS properly configured to allow requests from http://localhost:3000 (or your deployment URL).

### Authentication Fails

- Verify client credentials are correct
- Check that the client is enabled in the database
- Ensure the API Gateway is running and accessible

### API Connection Issues

- Verify the API base URL is correct
- Check that the API Gateway is running on the specified port
- Review network console for detailed error messages

## License

This project is part of the API Gateway repository.

