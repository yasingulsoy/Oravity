import apiClient from './client';

export type InvoiceIntegratorType = 'None' | 'Sovos' | 'DigitalPlanet' | 'Custom';

export interface BranchInvoiceSettings {
  branchId: number;
  integratorType: InvoiceIntegratorType;
  companyVkn: string | null;
  integratorEndpoint: string | null;
  integratorCompanyCode: string | null;
  integratorUsername: string | null;
  hasPassword: boolean;
  normalPrefix: string | null;
  normalCounter: number;
  eArchivePrefix: string | null;
  eArchiveCounter: number;
  eInvoicePrefix: string | null;
  eInvoiceCounter: number;
}

export interface UpdateBranchInvoiceSettingsRequest {
  integratorType: InvoiceIntegratorType;
  companyVkn?: string;
  integratorEndpoint?: string;
  integratorCompanyCode?: string;
  integratorUsername?: string;
  integratorPassword?: string;  // null → mevcut şifreyi değiştirme
  normalPrefix?: string;
  eArchivePrefix?: string;
  eInvoicePrefix?: string;
}

export const invoiceSettingsApi = {
  get: (branchId: number) =>
    apiClient.get<BranchInvoiceSettings>(`/settings/invoice/${branchId}`),

  update: (branchId: number, data: UpdateBranchInvoiceSettingsRequest) =>
    apiClient.put<BranchInvoiceSettings>(`/settings/invoice/${branchId}`, data),
};
