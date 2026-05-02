import apiClient from './client';

export interface ReferencePriceList {
  id: number;
  code: string;
  name: string;
  sourceType: string;
  year: number;
  isActive: boolean;
  itemCount: number;
}

export interface ReferencePriceItem {
  id: number;
  treatmentCode: string;
  treatmentName: string;
  price: number;
  priceKdv: number;
  currency: string;
  validFrom: string | null;
  validUntil: string | null;
}

export interface ReferencePriceItemsPage {
  items: ReferencePriceItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PricingRule {
  publicId: string;
  branchId: number | null;
  name: string;
  description: string | null;
  ruleType: string;
  priority: number;
  includeFilters: string | null;
  excludeFilters: string | null;
  formula: string | null;
  outputCurrency: string;
  validFrom: string | null;
  validUntil: string | null;
  isActive: boolean;
  stopProcessing: boolean;
}

export interface CreatePricingRulePayload {
  branchId?: number | null;
  name: string;
  description?: string | null;
  ruleType: string;
  priority: number;
  includeFilters?: string | null;
  excludeFilters?: string | null;
  formula?: string | null;
  outputCurrency: string;
  validFrom?: string | null;
  validUntil?: string | null;
  stopProcessing: boolean;
}

export interface UpdatePricingRulePayload extends CreatePricingRulePayload {
  isActive: boolean;
}

export interface BranchPricing {
  branchId: number;
  branchName: string;
  pricingMultiplier: number;
}

export interface TraceStep {
  phase: string;
  detail: string;
  result: string | null;
}

export interface TreatmentPriceResponse {
  unitPrice: number;
  referencePrice: number;
  currency: string;
  appliedRuleName: string | null;
  strategy: string;
  trace: TraceStep[] | null;
}

export const pricingApi = {
  // Reference Lists
  getReferenceLists: () =>
    apiClient.get<ReferencePriceList[]>('/pricing/reference-lists'),

  createReferenceList: (data: { code: string; name: string; sourceType?: string; year?: number }) =>
    apiClient.post<ReferencePriceList>('/pricing/reference-lists', { sourceType: 'private', year: new Date().getFullYear(), ...data }),

  getReferenceItems: (listId: number, params?: { search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<ReferencePriceItemsPage>(`/pricing/reference-lists/${listId}/items`, { params }),

  upsertReferenceItem: (listId: number, code: string, data: {
    treatmentName: string;
    price: number;
    priceKdv?: number;
    currency?: string;
  }) =>
    apiClient.put<ReferencePriceItem>(`/pricing/reference-lists/${listId}/items/${code}`, data),

  deleteReferenceItem: (listId: number, code: string) =>
    apiClient.delete(`/pricing/reference-lists/${listId}/items/${code}`),

  setReferenceListActive: (listId: number, isActive: boolean) =>
    apiClient.patch(`/pricing/reference-lists/${listId}/active`, { isActive }),

  deleteReferenceList: (listId: number) =>
    apiClient.delete(`/pricing/reference-lists/${listId}`),

  bulkUpsertReferenceItems: (listId: number, items: { code: string; name: string; price: number; priceKdv?: number; currency?: string }[]) =>
    apiClient.post<{ count: number }>(`/pricing/reference-lists/${listId}/items/bulk`, { items }),

  // Rules
  getRules: (activeOnly = false) =>
    apiClient.get<PricingRule[]>('/pricing/rules', { params: { activeOnly } }),

  createRule: (data: CreatePricingRulePayload) =>
    apiClient.post<PricingRule>('/pricing/rules', data),

  updateRule: (publicId: string, data: UpdatePricingRulePayload) =>
    apiClient.put<PricingRule>(`/pricing/rules/${publicId}`, data),

  deleteRule: (publicId: string) =>
    apiClient.delete(`/pricing/rules/${publicId}`),

  getTreatmentPrice: (treatmentPublicId: string, params?: { branchId?: number; institutionId?: number; isOss?: boolean; campaignCode?: string }) =>
    apiClient.get<TreatmentPriceResponse>(`/pricing/treatment/${treatmentPublicId}/price`, { params }),

  // Branch pricing
  getBranchPricing: () =>
    apiClient.get<BranchPricing[]>('/pricing/branches'),

  updateBranchMultiplier: (branchId: number, pricingMultiplier: number) =>
    apiClient.patch<BranchPricing>(`/pricing/branches/${branchId}/multiplier`, { pricingMultiplier }),
};
