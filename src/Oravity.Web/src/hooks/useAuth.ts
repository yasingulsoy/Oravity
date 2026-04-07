import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { authApi } from '@/api/auth';
import { useAuthStore } from '@/store/authStore';
import { extractUser } from '@/lib/jwt';
import type { LoginRequest } from '@/types/auth';

export function useLogin() {
  const navigate = useNavigate();
  const login = useAuthStore((s) => s.login);
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: ({ data }) => {
      const user = extractUser(data.accessToken);
      if (!user) return;

      // Önceki session'ın cache'ini temizle, yeni kullanıcının verisi gelsin
      queryClient.clear();
      login(data.accessToken, data.refreshToken, user);
      navigate('/dashboard');
    },
  });
}

export function useLogout() {
  const navigate = useNavigate();
  const logout = useAuthStore((s) => s.logout);
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      const { refreshToken } = useAuthStore.getState();
      await authApi.logout(refreshToken);
    },
    onSettled: () => {
      // Token ve store temizle
      logout();
      // Tüm cached sorgu verilerini sıfırla
      queryClient.clear();
      navigate('/');
    },
  });
}
