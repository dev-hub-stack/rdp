// API Response Types
export interface ApiResponse<T> {
  data: T
  message: string
  success: boolean
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  skip: number
  limit: number
}

// Authentication Types
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: User
}

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
  tenantId: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export enum UserRole {
  SystemAdmin = 'SystemAdmin',
  TenantAdmin = 'TenantAdmin',
  Operator = 'Operator'
}

// Tenant Types
export interface Tenant {
  id: string
  name: string
  domain: string
  isActive: boolean
  createdAt: string
  settings: TenantSettings
}

export interface TenantSettings {
  maxAgents: number
  maxSessions: number
  sessionTimeoutMinutes: number
  allowedIpRanges: string[]
}

// Agent Types
export interface Agent {
  id: string
  tenantId: string
  name: string
  hostname: string
  ipAddress: string
  operatingSystem: string
  version: string
  status: string
  lastSeen: string
  description?: string
  systemInfo: SystemInfo
}

export enum AgentStatus {
  Online = 'online',
  Offline = 'offline',
  InSession = 'in_session',
  Error = 'error'
}

export interface SystemInfo {
  computerName: string
  operatingSystem: string
  processorCount: number
  totalMemoryMB: number
  architecture: string
  rdpEnabled: boolean
}

// Session Types
export interface Session {
  id: string
  tenantId: string
  agentId: string
  userId: string
  connectCode: string
  status: SessionStatus
  createdAt: string
  startedAt?: string
  endedAt?: string
  durationSeconds?: number
  clientIpAddress: string
  agentName: string
  username: string
  agent: Agent
  user: User
}

export enum SessionStatus {
  Pending = 'pending',
  Active = 'active',
  Ended = 'ended',
  Error = 'error'
}

export interface CreateSessionRequest {
  agentId: string
  username: string
  durationMinutes?: number
}

export interface SessionConnectionInfo {
  connectCode: string
  relayEndpoint: string
  expiresAt: string
}

// Dashboard Types
export interface DashboardStats {
  totalAgents: number
  onlineAgents: number
  activeSessions: number
  totalUsers: number
  stats24h: {
    sessionsCreated: number
    sessionsCompleted: number
    averageDuration: number
  }
}

export interface AgentStatusCount {
  status: AgentStatus
  count: number
}

export interface SessionHistory {
  date: string
  sessionCount: number
  averageDuration: number
}

// Form Types
export interface CreateUserRequest {
  email: string
  firstName: string
  lastName: string
  role: string
  password: string
}

export interface UpdateUserRequest {
  email: string
  firstName: string
  lastName: string
  role: string
}

export interface CreateTenantRequest {
  name: string
  domain: string
  settings: TenantSettings
}

export interface CreateAgentRequest {
  name: string
  description?: string
  machineId: string
  machineName?: string
  ipAddress?: string
  rdpPort?: number
  groupIds?: string[]
  tags?: { [key: string]: string }
}

export interface UpdateAgentRequest {
  name: string
  description?: string
}

// WebSocket Types
export interface WebSocketMessage {
  type: string
  data: any
  timestamp: string
}

export interface AgentStatusUpdate {
  agentId: string
  status: AgentStatus
  timestamp: string
}

export interface SessionStatusUpdate {
  sessionId: string
  status: SessionStatus
  timestamp: string
}

// Error Types
export interface ApiError {
  message: string
  code: string
  details?: Record<string, any>
}

// Navigation Types
export interface NavigationItem {
  label: string
  path: string
  icon: string
  roles?: UserRole[]
}
