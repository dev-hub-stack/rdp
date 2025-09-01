import axios, { AxiosInstance, AxiosError } from 'axios'
import { useAuthStore } from '../stores/authStore'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

class ApiService {
  private axiosClient: AxiosInstance

  constructor() {
    this.axiosClient = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json'
      }
    })

    this.setupInterceptors()
  }

  private setupInterceptors() {
    // Request interceptor to add auth token
    this.axiosClient.interceptors.request.use(
      (config) => {
        const token = useAuthStore.getState().accessToken
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    // Response interceptor to handle token refresh
    this.axiosClient.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config

        if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
          originalRequest._retry = true

          try {
            await useAuthStore.getState().refreshAccessToken()
            const token = useAuthStore.getState().accessToken
            if (token) {
              originalRequest.headers!.Authorization = `Bearer ${token}`
            }
            return this.axiosClient(originalRequest)
          } catch (refreshError) {
            useAuthStore.getState().logout()
            window.location.href = '/login'
            return Promise.reject(refreshError)
          }
        }

        return Promise.reject(error)
      }
    )
  }

  public get client() {
    return this.axiosClient
  }
}

const apiService = new ApiService()
export const api = apiService.client

// Type augmentation for axios config
declare module 'axios' {
  interface AxiosRequestConfig {
    _retry?: boolean
  }
}
