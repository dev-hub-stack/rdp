import { api } from './api'
import { Session, CreateSessionRequest, PaginatedResponse } from '../types'
import { useAuthStore } from '../stores/authStore'

// Helper function to get tenant ID from current user
const getTenantId = (): string => {
  const user = useAuthStore.getState().user
  if (!user?.tenantId) {
    throw new Error('User tenant ID not available')
  }
  return user.tenantId
}

export const sessionsApi = {
  getSessions: async (page: number = 1, pageSize: number = 10, status?: string): Promise<PaginatedResponse<Session>> => {
    const tenantId = getTenantId()
    const params = new URLSearchParams({
      skip: ((page-1) * pageSize).toString(),
      limit: pageSize.toString()
    })
    
    if (status) {
      params.append('status', status)
    }
    
    const response = await api.get(`/tenants/${tenantId}/sessions?${params.toString()}`)
    return {
      items: response.data.items || [],
      total: response.data.total || 0,
      skip: response.data.skip || ((page-1) * pageSize),
      limit: response.data.limit || pageSize
    }
  },

  getSession: async (id: string): Promise<Session> => {
    const tenantId = getTenantId()
    const response = await api.get(`/tenants/${tenantId}/sessions/${id}`)
    return response.data
  },

  createSession: async (data: CreateSessionRequest): Promise<Session> => {
    const tenantId = getTenantId()
    const response = await api.post(`/tenants/${tenantId}/sessions`, data)
    return response.data
  },

  terminateSession: async (id: string): Promise<void> => {
    const tenantId = getTenantId()
    await api.delete(`/tenants/${tenantId}/sessions/${id}`)
  },

  getConnectInfo: async (connectCode: string): Promise<{ host: string; port: number }> => {
    const response = await api.get(`/sessions/connect/${connectCode}`)
    return response.data
  }
}
