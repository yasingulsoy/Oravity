import { useQuery } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { useAuthStore } from '@/store/authStore';

export function usePermissions() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const isPlatformAdmin = useAuthStore((s) => s.user?.isPlatformAdmin ?? false);

  const { data: permissions = [] } = useQuery({
    queryKey: ['my-permissions'],
    queryFn: () => authApi.myPermissions().then((r) => r.data),
    enabled: isAuthenticated && !isPlatformAdmin,
    staleTime: 5 * 60 * 1000,
  });

  const hasPermission = (code: string) => isPlatformAdmin || permissions.includes(code);

  return { permissions, hasPermission };
}
