import apiClient from './client';

/** Backend JsonStringEnumConverter ile string döner */
export type InstitutionPaymentModel = 'Discount' | 'Provision';

export interface InstitutionItem {
  id: number;
  publicId: string;
  name: string;
  code: string | null;
  type: string | null;
  marketSegment: 'domestic' | 'international' | null;
  paymentModel: InstitutionPaymentModel;
  phone: string | null;
  email: string | null;
  website: string | null;
  country: string | null;
  city: string | null;
  district: string | null;
  address: string | null;
  contactPerson: string | null;
  contactPhone: string | null;
  taxNumber: string | null;
  taxOffice: string | null;
  discountRate: number | null;
  paymentDays: number;
  paymentTerms: string | null;
  notes: string | null;
  isGlobal: boolean;
  isActive: boolean;
  // E-Fatura & Tevkifat
  isEInvoiceTaxpayer: boolean;
  withholdingApplies: boolean;
  withholdingCode: string | null;
  withholdingNumerator: number;
  withholdingDenominator: number;
}

export interface CreateInstitutionRequest {
  name: string;
  code?: string;
  type?: string;
  paymentModel: InstitutionPaymentModel;
  marketSegment?: 'domestic' | 'international';
  phone?: string;
  email?: string;
  website?: string;
  country?: string;
  city?: string;
  district?: string;
  address?: string;
  contactPerson?: string;
  contactPhone?: string;
  taxNumber?: string;
  taxOffice?: string;
  discountRate?: number;
  paymentDays?: number;
  paymentTerms?: string;
  notes?: string;
  isEInvoiceTaxpayer?: boolean;
  withholdingApplies?: boolean;
  withholdingCode?: string;
  withholdingNumerator?: number;
  withholdingDenominator?: number;
}

export interface UpdateInstitutionRequest extends CreateInstitutionRequest {
  isActive: boolean;
}

export const institutionsApi = {
  list: () =>
    apiClient.get<InstitutionItem[]>('/institutions'),

  create: (req: CreateInstitutionRequest) =>
    apiClient.post<InstitutionItem>('/institutions', req),

  update: (publicId: string, req: UpdateInstitutionRequest) =>
    apiClient.put<InstitutionItem>(`/institutions/${publicId}`, req),

  delete: (publicId: string) =>
    apiClient.delete(`/institutions/${publicId}`),
};
