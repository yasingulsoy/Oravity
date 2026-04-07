import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';

export function PrivateRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const accessToken = useAuthStore((s) => s.accessToken);
  const logout = useAuthStore((s) => s.logout);

  // localStorage'dan gelen token süresi dolmuşsa ve refresh token da yoksa direkt çıkart
  if (isAuthenticated && accessToken) {
    const payload = parseJwt(accessToken);
    if (payload && payload.exp * 1000 < Date.now()) {
      // Access token süresi dolmuş — refresh interceptor zaten dener,
      // ama refreshToken yoksa direkt logout yap
      const { refreshToken } = useAuthStore.getState();
      if (!refreshToken) {
        logout();
        return <Navigate to="/" replace />;
      }
    }
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
