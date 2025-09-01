import { api } from './api'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: {
    id: string
    email: string
    firstName: string
    lastName: string
    role: string
    tenantId: string
  }
}

export interface RefreshRequest {
  refreshToken: string
}

export interface RefreshResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: {
    id: string
    email: string
    firstName: string
    lastName: string
    role: string
    tenantId: string
  }
}

export const authApi = {
  login: async (email: string, password: string): Promise<LoginResponse> => {
    const response = await api.post('/auth/login', { email, password })
    return response.data
  },

  refresh: async (refreshToken: string): Promise<RefreshResponse> => {
    const response = await api.post('/auth/refresh', { refreshToken })
    return response.data
  },

  logout: async (): Promise<void> => {
    await api.post('/auth/logout')
  }
}
