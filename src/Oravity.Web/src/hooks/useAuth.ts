import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { authApi } from '@/api/auth';
import { useAuthStore } from '@/store/authStore';
import { extractUser } from '@/lib/jwt';
import type { LoginRequest, BranchOption } from '@/types/auth';

export function useLogin() {
  const navigate = useNavigate();
  const login = useAuthStore((s) => s.login);
  const queryClient = useQueryClient();
  const [pendingBranches, setPendingBranches] = useState<BranchOption[] | null>(null);
  const [pendingCredentials, setPendingCredentials] = useState<{ email: string; password: string } | null>(null);

  const mutation = useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: ({ data }) => {
      if (data.requiresBranchSelection && data.branches) {
        setPendingBranches(data.branches);
        return;
      }

      if (!data.accessToken || !data.refreshToken) return;

      const user = extractUser(data.accessToken);
      if (!user) return;

      // Önceki session'ın cache'ini temizle, yeni kullanıcının verisi gelsin
      queryClient.clear();
      login(data.accessToken, data.refreshToken, user);
      navigate('/dashboard');
    },
  });

  const submitCredentials = (data: { email: string; password: string }) => {
    setPendingCredentials(data);
    mutation.mutate(data);
  };

  const selectBranch = (branchId: number) => {
    if (!pendingCredentials) return;
    setPendingBranches(null);
    mutation.mutate({ ...pendingCredentials, branchId });
  };

  const clearBranchSelection = () => {
    setPendingBranches(null);
  };

  return { mutation, pendingBranches, submitCredentials, selectBranch, clearBranchSelection };
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
