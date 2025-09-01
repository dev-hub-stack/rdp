import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { User } from '../types'
import { authApi } from '../services/authApi'

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  refreshAccessToken: () => Promise<void>
  setUser: (user: User) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      login: async (email: string, password: string) => {
        try {
          const response = await authApi.login(email, password)
          
          const user: User = {
            id: response.user.id,
            email: response.user.email,
            tenantId: response.user.tenantId,
            firstName: response.user.firstName,
            lastName: response.user.lastName,
            role: response.user.role,
            isActive: true,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
          }

          set({
            user,
            accessToken: response.token,
            refreshToken: response.refreshToken,
            isAuthenticated: true
          })
        } catch (error) {
          throw error
        }
      },

      logout: () => {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false
        })
      },

      refreshAccessToken: async () => {
        const { refreshToken } = get()
        if (!refreshToken) {
          throw new Error('No refresh token available')
        }

        try {
          const response = await authApi.refresh(refreshToken)
          
          const user: User = {
            id: response.user.id,
            email: response.user.email,
            tenantId: response.user.tenantId,
            firstName: response.user.firstName,
            lastName: response.user.lastName,
            role: response.user.role,
            isActive: true,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
          }

          set({
            user,
            accessToken: response.token,
            refreshToken: response.refreshToken,
            isAuthenticated: true
          })
        } catch (error) {
          get().logout()
          throw error
        }
      },

      setUser: (user: User) => {
        set({ user })
      }
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated
      })
    }
  )
)
