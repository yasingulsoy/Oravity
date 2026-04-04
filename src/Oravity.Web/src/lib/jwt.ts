import type { AuthUser } from '@/types/auth';

interface JwtPayload {
  sub: string;
  email: string;
  user_id: string;
  public_id: string;
  full_name: string;
  is_platform_admin: string;
  exp: number;
}

export function parseJwt(token: string): JwtPayload | null {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

export function extractUser(accessToken: string): AuthUser | null {
  const payload = parseJwt(accessToken);
  if (!payload) return null;

  return {
    id: payload.user_id ?? payload.sub,
    email: payload.email,
    name: payload.full_name,
    publicId: payload.public_id,
    isPlatformAdmin: payload.is_platform_admin === 'true',
  };
}
