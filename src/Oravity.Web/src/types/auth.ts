export interface BranchOption {
  id: number;
  name: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  branchId?: number;
}

export interface LoginResponse {
  accessToken?: string;
  refreshToken?: string;
  expiresIn: number;
  tokenType: string;
  requiresBranchSelection?: boolean;
  branches?: BranchOption[];
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
