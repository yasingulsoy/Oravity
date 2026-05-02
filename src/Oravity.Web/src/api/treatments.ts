import apiClient from './client';
import type { TreatmentPlan, TreatmentPlanItem } from '@/types/treatment';

export interface TreatmentCatalogItem {
  publicId: string;
  code: string;
  sutCode: string | null;
  name: string;
  category: { publicId: string; name: string } | null;
  kdvRate: number;
  costPrice: number | null;
  requiresSurfaceSelection: boolean;
  requiresLaboratory: boolean;
  isGlobal: boolean;
  isActive: boolean;
  chartSymbolCode: string | null;
}

export interface PagedTreatmentResponse {
  items: TreatmentCatalogItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface TreatmentPriceResponse {
  unitPrice: number;
  referencePrice: number;
  currency: string;
  appliedRuleName: string | null;
  strategy: 'Rule' | 'ReferencePrice' | 'NoPriceConfigured';
}

export interface TreatmentDetail {
  publicId: string;
  code: string;
  sutCode: string | null;
  name: string;
  category: { publicId: string; name: string } | null;
  tags: string | null;
  kdvRate: number;
  costPrice: number | null;
  requiresSurfaceSelection: boolean;
  requiresLaboratory: boolean;
  allowedScopes: number[];
  isActive: boolean;
  isGlobal: boolean;
  createdAt: string;
  chartSymbolCode: string | null;
}

export interface TreatmentCategory {
  publicId: string;
  name: string;
  parentPublicId: string | null;
  sortOrder: number;
  isActive: boolean;
}

export const treatmentCategoriesApi = {
  list: () =>
    apiClient.get<TreatmentCategory[]>('/treatment-categories'),
};

export const treatmentsApi = {
  list: (params?: { search?: string; categoryId?: string; page?: number; pageSize?: number; activeOnly?: boolean }) =>
    apiClient.get<PagedTreatmentResponse>('/treatments', { params: { activeOnly: true, pageSize: 200, ...params } }),

  getById: (publicId: string) =>
    apiClient.get<TreatmentDetail>(`/treatments/${publicId}`),

  create: (data: {
    code: string; name: string; categoryPublicId?: string | null;
    kdvRate: number; requiresSurfaceSelection: boolean;
    requiresLaboratory: boolean; allowedScopes?: number[] | null; tags?: string | null;
    costPrice?: number | null;
  }) =>
    apiClient.post<TreatmentDetail>('/treatments', data),

  update: (publicId: string, data: {
    code: string; name: string; categoryPublicId?: string | null;
    kdvRate: number; requiresSurfaceSelection: boolean;
    requiresLaboratory: boolean; allowedScopes?: number[] | null; tags?: string | null;
    isActive: boolean;
    costPrice?: number | null;
  }) =>
    apiClient.put<TreatmentDetail>(`/treatments/${publicId}`, data),

  getPrice: (publicId: string, opts?: { branchId?: number; institutionId?: number; isOss?: boolean; campaignCode?: string }) =>
    apiClient.get<TreatmentPriceResponse>(`/pricing/treatment/${publicId}/price`, {
      params: {
        ...(opts?.branchId      ? { branchId:      opts.branchId }      : {}),
        ...(opts?.institutionId ? { institutionId: opts.institutionId } : {}),
        ...(opts?.isOss         ? { isOss:         true }               : {}),
        ...(opts?.campaignCode  ? { campaignCode:  opts.campaignCode }  : {}),
      },
    }),
};

export interface TreatmentMapping {
  id: number;
  internalTreatmentId: number;
  internalTreatmentCode: string;
  internalTreatmentName: string;
  referenceListId: number;
  referenceListCode: string;
  referenceCode: string;
  referenceItemName: string | null;
  mappingQuality: string | null;
  notes: string | null;
}

export const treatmentMappingsApi = {
  getMappings: (treatmentPublicId: string) =>
    apiClient.get<TreatmentMapping[]>(`/treatments/${treatmentPublicId}/mappings`),

  createMapping: (treatmentPublicId: string, data: {
    referenceListId: number;
    referenceCode: string;
    mappingQuality?: string | null;
    notes?: string | null;
  }) =>
    apiClient.post<TreatmentMapping>(`/treatments/${treatmentPublicId}/mappings`, data),

  deleteMapping: (treatmentPublicId: string, mappingId: number) =>
    apiClient.delete(`/treatments/${treatmentPublicId}/mappings/${mappingId}`),
};

export const treatmentPlansApi = {
  getByPatient: (patientPublicId: string) =>
    apiClient.get<TreatmentPlan[]>(`/patients/${patientPublicId}/treatment-plans`),

  create: (data: { patientPublicId: string; doctorPublicId: string; name: string; notes?: string; institutionId?: number }) =>
    apiClient.post<TreatmentPlan>('/treatment-plans', data),

  approve: (planPublicId: string) =>
    apiClient.put<TreatmentPlan>(`/treatment-plans/${planPublicId}/approve`),

  approveItems: (planPublicId: string, itemPublicIds: string[]) =>
    apiClient.put<TreatmentPlan>(`/treatment-plans/${planPublicId}/items/approve`, { itemPublicIds }),

  addItem: (planPublicId: string, data: {
    treatmentPublicId: string;
    unitPrice: number;
    discountRate: number;
    toothNumber?: string;
    notes?: string;
    priceCurrency?: string;
    priceExchangeRate?: number;
    listPrice?: number;
  }) =>
    apiClient.post<TreatmentPlanItem>(`/treatment-plans/${planPublicId}/items`, data),

  deleteItem: (planPublicId: string, itemPublicId: string) =>
    apiClient.delete(`/treatment-plans/${planPublicId}/items/${itemPublicId}`),

  completeItem: (planPublicId: string, itemPublicId: string) =>
    apiClient.put<TreatmentPlanItem>(`/treatment-plans/${planPublicId}/items/${itemPublicId}/complete`),

  revertItem: (planPublicId: string, itemPublicId: string, reason: string) =>
    apiClient.put<TreatmentPlanItem>(`/treatment-plans/${planPublicId}/items/${itemPublicId}/revert`, { reason }),

  revertToPlanned: (planPublicId: string, itemPublicId: string) =>
    apiClient.put<TreatmentPlanItem>(`/treatment-plans/${planPublicId}/items/${itemPublicId}/revert-to-planned`),

  update: (planPublicId: string, data: { name: string; notes?: string | null }) =>
    apiClient.put<TreatmentPlan>(`/treatment-plans/${planPublicId}`, data),

  deletePlan: (planPublicId: string) =>
    apiClient.delete(`/treatment-plans/${planPublicId}`),

  updateItem: (planPublicId: string, itemPublicId: string, data: {
    unitPrice: number;
    discountRate: number;
    toothNumber?: string | null;
  }) =>
    apiClient.put<TreatmentPlanItem>(`/treatment-plans/${planPublicId}/items/${itemPublicId}`, data),

  setContribution: (planPublicId: string, itemPublicId: string, contributionAmount: number | null) =>
    apiClient.put<TreatmentPlanItem>(
      `/treatment-plans/${planPublicId}/items/${itemPublicId}/contribution`,
      { contributionAmount },
    ),

  downloadPdf: (planPublicId: string, currency?: string) =>
    apiClient.get(`/treatment-plans/${planPublicId}/pdf`, {
      responseType: 'blob',
      params: currency ? { currency } : undefined,
    }),
};
