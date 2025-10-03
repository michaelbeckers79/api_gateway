# Admin Application Documentation

The API Gateway includes a modern web-based admin application built with Next.js and shadcn/ui for managing your gateway configuration.

## Overview

The admin application provides a user-friendly interface to:
- Manage API routes and their configurations
- Configure backend server clusters
- View and manage users and their sessions
- Manage client credentials for API access
- Test OAuth authentication flows (Token Handler Pattern)

## Quick Start

1. Navigate to the admin application directory:
   ```bash
   cd admin-app
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Run the development server:
   ```bash
   npm run dev
   ```

4. Open http://localhost:3000 in your browser

5. Sign in with your admin client credentials

## Features

### 📊 Dashboard
- Overview of system statistics
- Quick access to all management sections
- Professional light-themed interface

### 🛣️ Routes Management
- List all configured routes
- Create, edit, and delete routes
- Configure route patterns, clusters, and priorities
- Toggle route active status

### 🖥️ Clusters Management
- Manage backend server clusters
- Add and configure destination addresses
- Enable/disable clusters
- Real-time cluster status

### 👥 Users Management
- View all registered users
- See detailed user information
- View and manage active sessions
- Enable/disable user accounts
- Revoke individual or all user sessions

### 🔑 Clients Management
- Manage API client credentials
- Create new clients with secure secrets
- Enable/disable client access
- Track last usage timestamps

### 🔐 Token Handler Testing
- Interactive OAuth flow testing
- Start authentication flows
- Check login status
- Test logout functionality
- Built-in documentation

## Authentication

The admin application uses HTTP Basic Authentication with client credentials. Make sure to:

1. Create an admin client in the database (see main README)
2. Use strong, randomly generated client secrets
3. Store credentials securely
4. Rotate credentials regularly

## Production Deployment

For production use:

1. Build the application:
   ```bash
   cd admin-app
   npm run build
   ```

2. Start the production server:
   ```bash
   npm start
   ```

3. Configure environment variables:
   ```env
   NEXT_PUBLIC_API_BASE_URL=https://your-api-gateway.com
   ```

4. Set up HTTPS and proper CORS configuration

## Technology Stack

- **Next.js 15** - React framework with App Router
- **TypeScript** - Type-safe development
- **shadcn/ui** - Beautiful, accessible UI components
- **Tailwind CSS** - Utility-first styling
- **Lucide React** - Modern icon library

## Security Best Practices

- ✅ Use HTTPS in production
- ✅ Restrict admin panel access to trusted networks
- ✅ Regularly rotate admin credentials
- ✅ Monitor admin API access logs
- ✅ Keep dependencies up to date
- ✅ Configure proper CORS policies

## Documentation

For detailed information, see:
- [Admin Application README](admin-app/README.md) - Comprehensive guide
- [ADMIN_API.md](ADMIN_API.md) - API endpoint documentation
- [SECURITY.md](SECURITY.md) - Security guidelines

## Screenshots

### Login Page
Clean and professional authentication interface.

### Dashboard
Overview of your API Gateway with quick access to all features.

### Token Handler Testing
Interactive testing interface for OAuth authentication flows.

## Support

For issues, questions, or contributions, please refer to the main repository documentation.
