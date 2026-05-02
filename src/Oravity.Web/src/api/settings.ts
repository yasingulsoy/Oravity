import apiClient from './client';

// ─── Şirket ──────────────────────────────────────────────────────────────────

export interface CompanyInfo {
  publicId: string;
  name: string;
  defaultLanguageCode: string;
  isActive: boolean;
  subscriptionEndsAt: string | null;
  verticalName: string;
}

export interface UpdateCompanyPayload {
  name?: string;
  defaultLanguageCode?: string;
}

// ─── Şube ────────────────────────────────────────────────────────────────────

export interface BranchItem {
  publicId: string;
  name: string;
  defaultLanguageCode: string;
  isActive: boolean;
  pricingMultiplier: number;
  activeUserCount: number;
  createdAt: string;
  companyName?: string;
}

export interface BranchUserInfo {
  publicId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  title: string | null;
  roleName: string;
  roleCode: string;
}

export interface BranchDetail {
  publicId: string;
  name: string;
  defaultLanguageCode: string;
  isActive: boolean;
  pricingMultiplier: number;
  verticalId: number | null;
  verticalName: string | null;
  createdAt: string;
  updatedAt: string | null;
  activeUserCount: number;
  users: BranchUserInfo[];
}

export interface CreateBranchPayload {
  name: string;
  defaultLanguageCode?: string;
}

export interface UpdateBranchPayload {
  name?: string;
  defaultLanguageCode?: string;
  isActive?: boolean;
  pricingMultiplier?: number;
}

// ─── Kullanıcı ───────────────────────────────────────────────────────────────

export interface UserRoleInfo {
  roleName: string;
  roleCode: string;
  branchName: string | null;
}

export interface UserItem {
  publicId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isPlatformAdmin: boolean;
  title: string | null;
  lastLoginAt: string | null;
  roles: UserRoleInfo[];
}

export interface UserRoleAssignment {
  publicId: string;
  roleCode: string;
  roleName: string;
  branchId: number | null;
  branchName: string | null;
  companyId: number | null;
  isActive: boolean;
  assignedAt: string;
  expiresAt: string | null;
}

export interface UserDetail {
  publicId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isPlatformAdmin: boolean;
  title: string | null;
  specializationId: number | null;
  specializationName: string | null;
  calendarColor: string | null;
  defaultAppointmentDuration: number | null;
  isChiefPhysician: boolean;
  preferredLanguageCode: string | null;
  lastLoginAt: string | null;
  roleAssignments: UserRoleAssignment[];
}

export interface CreateUserPayload {
  email: string;
  fullName: string;
  password: string;
  roleCode?: string;
  branchPublicId?: string;
  title?: string;
  calendarColor?: string;
  defaultAppointmentDuration?: number;
}

export interface UpdateUserPayload {
  fullName?: string;
  isActive?: boolean;
  title?: string;
  specializationId?: number; // 0 = clear
  calendarColor?: string;
  defaultAppointmentDuration?: number;
  preferredLanguageCode?: string;
}

export interface AssignRolePayload {
  roleCode: string;
  branchPublicId?: string;
}

// ─── Roller & İzinler ────────────────────────────────────────────────────────

export interface RoleItem {
  publicId: string;
  code: string;
  name: string;
  description: string | null;
  permissions: string[];
  activeUserCount: number;
}

export interface PermissionItem {
  publicId: string;
  code: string;
  resource: string;
  action: string;
  isDangerous: boolean;
}

// ─── Güvenlik Politikası ─────────────────────────────────────────────────────

export interface SecurityPolicy {
  branchPublicId: string;
  twoFaRequired: boolean;
  twoFaSkipInternalIp: boolean;
  allowedIpRanges: string | null;
  sessionTimeoutMinutes: number;
  maxFailedAttempts: number;
  lockoutMinutes: number;
}

export interface UpdateSecurityPolicyPayload {
  twoFaRequired: boolean;
  twoFaSkipInternalIp: boolean;
  allowedIpRanges?: string | null;
  sessionTimeoutMinutes: number;
  maxFailedAttempts: number;
  lockoutMinutes: number;
}

// ─── Uzmanlık Alanları ───────────────────────────────────────────────────────

export interface SpecializationItem {
  id: number;
  name: string;
  code: string;
}

export interface BankItem {
  publicId: string;
  name: string;
  shortName: string;
  bicCode?: string;
  isActive: boolean;
}

export interface CreateBankPayload {
  name: string;
  shortName: string;
  bicCode?: string;
}

export interface PaymentProviderItem {
  publicId: string;
  name: string;
  shortName?: string;
  website?: string;
  isActive: boolean;
}

export interface PosTerminalItem {
  publicId: string;
  name: string;
  bankId?: number;
  bankPublicId?: string;
  bankShortName?: string;
  terminalId?: string;
  isActive: boolean;
}

export interface CreatePosTerminalPayload {
  name: string;
  bankPublicId?: string;
  terminalId?: string;
}

export interface BankAccountItem {
  publicId: string;
  bankId?: number;
  bankPublicId?: string;
  bankShortName?: string;
  accountName: string;
  iban?: string;
  currency: string;
  isActive: boolean;
}

export interface CreateBankAccountPayload {
  bankPublicId?: string;
  accountName: string;
  iban?: string;
  currency?: string;
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const settingsApi = {
  // Şirket
  getCompany: () =>
    apiClient.get<CompanyInfo>('/settings/company'),
  updateCompany: (data: UpdateCompanyPayload) =>
    apiClient.put<CompanyInfo>('/settings/company', data),

  // Şubeler
  listBranches: () =>
    apiClient.get<BranchItem[]>('/settings/branches'),
  getBranch: (publicId: string) =>
    apiClient.get<BranchDetail>(`/settings/branches/${publicId}`),
  createBranch: (data: CreateBranchPayload) =>
    apiClient.post<BranchItem>('/settings/branches', data),
  updateBranch: (publicId: string, data: UpdateBranchPayload) =>
    apiClient.put<BranchItem>(`/settings/branches/${publicId}`, data),
  deleteBranch: (publicId: string) =>
    apiClient.delete(`/settings/branches/${publicId}`),
  listBranchUsers: (publicId: string) =>
    apiClient.get<BranchUserInfo[]>(`/settings/branches/${publicId}/users`),

  // Kullanıcılar
  listUsers: () =>
    apiClient.get<UserItem[]>('/settings/users'),
  getUser: (publicId: string) =>
    apiClient.get<UserDetail>(`/settings/users/${publicId}`),
  createUser: (data: CreateUserPayload) =>
    apiClient.post('/settings/users', data),
  updateUser: (publicId: string, data: UpdateUserPayload) =>
    apiClient.put(`/settings/users/${publicId}`, data),
  deleteUser: (publicId: string) =>
    apiClient.delete(`/settings/users/${publicId}`),
  assignRole: (userPublicId: string, data: AssignRolePayload) =>
    apiClient.post(`/settings/users/${userPublicId}/roles`, data),
  revokeRole: (userPublicId: string, assignmentPublicId: string) =>
    apiClient.delete(`/settings/users/${userPublicId}/roles/${assignmentPublicId}`),

  // Roller & İzinler
  listRoles: () =>
    apiClient.get<RoleItem[]>('/settings/roles'),
  listPermissions: () =>
    apiClient.get<PermissionItem[]>('/settings/permissions'),

  // Güvenlik Politikası
  getSecurityPolicy: (branchPublicId: string) =>
    apiClient.get<SecurityPolicy>(`/settings/branches/${branchPublicId}/security-policy`),
  updateSecurityPolicy: (branchPublicId: string, data: UpdateSecurityPolicyPayload) =>
    apiClient.put<SecurityPolicy>(`/settings/branches/${branchPublicId}/security-policy`, data),

  // Uzmanlık Alanları
  listSpecializations: () =>
    apiClient.get<SpecializationItem[]>('/settings/specializations'),

  // Bankalar (global referans)
  listBanks: () =>
    apiClient.get<BankItem[]>('/settings/banks'),
  createBank: (data: CreateBankPayload) =>
    apiClient.post<BankItem>('/settings/banks', data),
  updateBank: (publicId: string, data: CreateBankPayload) =>
    apiClient.put<BankItem>(`/settings/banks/${publicId}`, data),

  // Ödeme Sağlayıcıları (global referans)
  listPaymentProviders: () =>
    apiClient.get<PaymentProviderItem[]>('/settings/payment-providers'),

  // POS Cihazları
  listPosTerminals: (branchId?: string) =>
    apiClient.get<PosTerminalItem[]>('/settings/pos-terminals', { params: { branchId } }),
  createPosTerminal: (data: CreatePosTerminalPayload, branchId?: string) =>
    apiClient.post<PosTerminalItem>('/settings/pos-terminals', data, { params: { branchId } }),
  updatePosTerminal: (publicId: string, data: CreatePosTerminalPayload) =>
    apiClient.put<PosTerminalItem>(`/settings/pos-terminals/${publicId}`, data),
  deletePosTerminal: (publicId: string) =>
    apiClient.delete(`/settings/pos-terminals/${publicId}`),

  // Banka Hesapları
  listBankAccounts: (branchId?: string) =>
    apiClient.get<BankAccountItem[]>('/settings/bank-accounts', { params: { branchId } }),
  createBankAccount: (data: CreateBankAccountPayload, branchId?: string) =>
    apiClient.post<BankAccountItem>('/settings/bank-accounts', data, { params: { branchId } }),
  updateBankAccount: (publicId: string, data: CreateBankAccountPayload) =>
    apiClient.put<BankAccountItem>(`/settings/bank-accounts/${publicId}`, data),
  deleteBankAccount: (publicId: string) =>
    apiClient.delete(`/settings/bank-accounts/${publicId}`),
};
