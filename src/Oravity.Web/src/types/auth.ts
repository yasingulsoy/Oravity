export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  publicId: string;
  isPlatformAdmin: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
