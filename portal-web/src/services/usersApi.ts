import { api } from './api'
import { User, CreateUserRequest, UpdateUserRequest, PaginatedResponse } from '../types'
import { useAuthStore } from '../stores/authStore'

// Helper function to get tenant ID from current user
const getTenantId = (): string => {
  const user = useAuthStore.getState().user
  if (!user?.tenantId) {
    throw new Error('User tenant ID not available')
  }
  return user.tenantId
}

export const usersApi = {
  getUsers: async (page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<User>> => {
    const tenantId = getTenantId()
    const skip = (page - 1) * pageSize
    const params = new URLSearchParams({
      skip: skip.toString(),
      limit: pageSize.toString()
    })
    
    const response = await api.get(`/tenants/${tenantId}/users?${params.toString()}`)
    return {
      items: response.data.items || [],
      total: response.data.total || 0,
      skip: response.data.skip || skip,
      limit: response.data.limit || pageSize
    }
  },

  getUser: async (id: string): Promise<User> => {
    const tenantId = getTenantId()
    const response = await api.get(`/tenants/${tenantId}/users/${id}`)
    return response.data
  },

  createUser: async (data: CreateUserRequest): Promise<User> => {
    const tenantId = getTenantId()
    const response = await api.post(`/tenants/${tenantId}/users`, data)
    return response.data
  },

  updateUser: async (id: string, data: UpdateUserRequest): Promise<User> => {
    const tenantId = getTenantId()
    const response = await api.put(`/tenants/${tenantId}/users/${id}`, data)
    return response.data
  },

  deleteUser: async (id: string): Promise<void> => {
    const tenantId = getTenantId()
    await api.delete(`/tenants/${tenantId}/users/${id}`)
  },

  changePassword: async (currentPassword: string, newPassword: string): Promise<void> => {
    const tenantId = getTenantId()
    await api.post(`/tenants/${tenantId}/users/change-password`, {
      currentPassword,
      newPassword
    })
  }
}
