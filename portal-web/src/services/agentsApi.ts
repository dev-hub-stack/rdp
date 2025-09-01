import { api } from './api'
import { Agent, CreateAgentRequest, UpdateAgentRequest, PaginatedResponse } from '../types'
import { useAuthStore } from '../stores/authStore'

// Helper function to get tenant ID from current user
const getTenantId = (): string => {
  const user = useAuthStore.getState().user
  if (!user?.tenantId) {
    throw new Error('User tenant ID not available')
  }
  return user.tenantId
}

export const agentsApi = {
  getAgents: async (page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<Agent>> => {
    const tenantId = getTenantId()
    const params = new URLSearchParams({
      skip: ((page-1) * pageSize).toString(),
      limit: pageSize.toString()
    })
    
    const response = await api.get(`/tenants/${tenantId}/agents?${params.toString()}`)
    return {
      items: response.data.items || [],
      total: response.data.total || 0,
      skip: response.data.skip || ((page-1) * pageSize),
      limit: response.data.limit || pageSize
    }
  },

  getAgent: async (id: string): Promise<Agent> => {
    const tenantId = getTenantId()
    const response = await api.get(`/tenants/${tenantId}/agents/${id}`)
    return response.data
  },

  createAgent: async (data: CreateAgentRequest): Promise<Agent> => {
    const tenantId = getTenantId()
    const response = await api.post(`/tenants/${tenantId}/agents`, data)
    return response.data
  },

  updateAgent: async (id: string, data: UpdateAgentRequest): Promise<Agent> => {
    const tenantId = getTenantId()
    const response = await api.put(`/tenants/${tenantId}/agents/${id}`, data)
    return response.data
  },

  deleteAgent: async (id: string): Promise<void> => {
    const tenantId = getTenantId()
    await api.delete(`/tenants/${tenantId}/agents/${id}`)
  },

  generateProvisioningToken: async (groupId?: string): Promise<{ token: string; expiresAt: string }> => {
    const tenantId = getTenantId()
    const response = await api.post(`/tenants/${tenantId}/agents/provisioning-token`, {
      groupId: groupId || null
    })
    return response.data
  },

  getAgentStatus: async (id: string): Promise<{ status: string; lastSeen: string }> => {
    const tenantId = getTenantId()
    const response = await api.get(`/tenants/${tenantId}/agents/${id}/status`)
    return response.data
  }
}
