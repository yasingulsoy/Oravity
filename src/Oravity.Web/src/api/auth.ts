import apiClient from './client';
import type { LoginRequest, LoginResponse } from '@/types/auth';

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<LoginResponse>('/auth/login', data),

  logout: (refreshToken: string | null) =>
    apiClient.post('/auth/logout', { refreshToken }),

  me: () =>
    apiClient.get<LoginResponse['user']>('/auth/me'),

  myPermissions: () =>
    apiClient.get<string[]>('/auth/my-permissions'),
};
