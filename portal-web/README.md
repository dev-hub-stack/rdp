# RDP Relay Portal Web

React/TypeScript frontend for the RDP Relay platform.

## Features

- **Multi-tenant Dashboard**: Secure operator interface for multiple tenants
- **Agent Management**: Real-time monitoring and control of Windows agents  
- **Session Management**: Initiate, monitor and terminate RDP sessions
- **User Management**: Role-based access control and user administration
- **Audit Logging**: Comprehensive activity logs and session history

## Technology Stack

- **React 18** with TypeScript
- **Vite** for fast development and building
- **Material-UI v5** for modern, responsive UI components
- **React Router v6** for client-side routing
- **React Query (TanStack Query)** for efficient API state management
- **Axios** for HTTP client with JWT token management
- **Socket.IO Client** for real-time updates
- **React Hook Form** with Zod validation
- **Chart.js** for analytics and monitoring dashboards

## Development Setup

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Project Structure

```
src/
├── components/         # Reusable UI components
│   ├── common/        # Shared components (buttons, modals, etc.)
│   ├── layout/        # Layout components (header, sidebar, etc.)
│   └── forms/         # Form components
├── pages/             # Page components
│   ├── auth/          # Authentication pages
│   ├── dashboard/     # Dashboard pages
│   ├── agents/        # Agent management pages
│   ├── sessions/      # Session management pages
│   └── users/         # User management pages
├── hooks/             # Custom React hooks
├── services/          # API services and utilities
├── stores/            # State management
├── types/             # TypeScript type definitions
├── utils/             # Utility functions
└── styles/            # Global styles and themes
```

## Environment Variables

Create a `.env` file:

```bash
VITE_API_BASE_URL=https://localhost:5000
VITE_RELAY_WS_URL=wss://localhost:5001
VITE_APP_TITLE=RDP Relay Portal
```

## Building and Deployment

The frontend is designed to be deployed as static files behind a reverse proxy (nginx, Apache, etc.) or via CDN.

Production build outputs to `dist/` directory.

## Features Roadmap

### Phase 1 (Current)
- [ ] Authentication and authorization
- [ ] Basic dashboard with metrics
- [ ] Agent list and status monitoring
- [ ] Session initiation and termination

### Phase 2
- [ ] Real-time session monitoring
- [ ] User and tenant management
- [ ] Audit logging and reporting
- [ ] Advanced agent configuration

### Phase 3
- [ ] Advanced analytics and reporting
- [ ] Session recording and playback
- [ ] Multi-factor authentication
- [ ] API key management

## Security Features

- JWT-based authentication with refresh tokens
- Role-based access control (RBAC)
- CSRF protection
- XSS protection via Content Security Policy
- Secure HTTP headers
- API request rate limiting
- Session timeout management

## Browser Support

- Chrome/Chromium 88+
- Firefox 78+
- Safari 14+
- Edge 88+
