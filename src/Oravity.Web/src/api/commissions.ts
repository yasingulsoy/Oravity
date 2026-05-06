import apiClient from './client';

// ── Enums (backend JsonStringEnumConverter ile string olarak gelir) ────

export type WorkingStyle = 'Accrual' | 'Collection';

export const WORKING_STYLE_LABEL: Record<WorkingStyle, string> = {
  Accrual:    'Tahakkuk (Tedavi yapılınca)',
  Collection: 'Tahsilat (Ödeme alınınca)',
};

export const PAYMENT_TYPE_LABEL: Record<PaymentType, string> = {
  Fix:                       'Sabit',
  Prim:                      'Prim (%)',
  FixPlusPrim:               'Sabit + Prim',
  PerJob:                    'İş Başı',
  PerJobSelectedPlusFixPrim: 'Seçili İş Başı + Fix/Prim',
  PriceRange:                'Fiyat Bandı',
};

export type PaymentType =
  | 'Fix'
  | 'Prim'
  | 'FixPlusPrim'
  | 'PerJob'
  | 'PerJobSelectedPlusFixPrim'
  | 'PriceRange';

export type JobStartCalculation = 'FromPriceList' | 'CustomPrices';

export type JobStartPriceType = 'FixedAmount' | 'Percentage';

// ── Commission Templates ───────────────────────────────────────────────

export interface JobStartPriceResponse {
  id: number;
  treatmentId: number;
  priceType: JobStartPriceType;
  value: number;
}

export interface JobStartPriceRequest {
  treatmentId: number;
  priceType: JobStartPriceType;
  value: number;
}

export interface PriceRangeResponse {
  id: number;
  minAmount: number;
  maxAmount: number | null;
  rate: number;
}

export interface PriceRangeRequest {
  minAmount: number;
  maxAmount: number | null;
  rate: number;
}

/**
 * Backend DoctorCommissionTemplate → CommissionTemplateResponse.
 * Tüm oranlar 0-100 aralığında yüzde değer olarak tutulur (SPEC 9126: DECIMAL(5,2)).
 */
export interface CommissionTemplate {
  publicId: string;
  id: number;
  name: string;

  workingStyle: WorkingStyle;
  workingStyleLabel: string;

  paymentType: PaymentType;
  paymentTypeLabel: string;

  jobStartCalculation: JobStartCalculation | null;

  fixedFee: number;
  primRate: number;

  clinicTargetEnabled: boolean;
  clinicTargetBonusRate: number | null;
  doctorTargetEnabled: boolean;
  doctorTargetBonusRate: number | null;

  /** SPEC 9136: true=Fatura kesilince, false=Kurum ödeyince. */
  institutionPayOnInvoice: boolean;

  deductTreatmentPlanCommission: boolean;
  deductLabCost: boolean;
  deductTreatmentCost: boolean;
  requireLabApproval: boolean;
  deductCreditCardCommission: boolean;

  kdvEnabled: boolean;
  kdvRate: number | null;
  /** JSON array string; ör: "[1,2,3]". */
  kdvAppliedPaymentTypes: string | null;

  extraExpenseEnabled: boolean;
  extraExpenseRate: number | null;

  withholdingTaxEnabled: boolean;
  withholdingTaxRate: number | null;

  isActive: boolean;
  jobStartPrices: JobStartPriceResponse[];
  priceRanges: PriceRangeResponse[];
  createdAt: string;
}

export interface CommissionTemplateInput {
  name: string;
  workingStyle: WorkingStyle;
  paymentType: PaymentType;
  fixedFee: number;
  primRate: number;
  institutionPayOnInvoice: boolean;
  jobStartCalculation: JobStartCalculation | null;

  clinicTargetEnabled: boolean;
  clinicTargetBonusRate: number | null;
  doctorTargetEnabled: boolean;
  doctorTargetBonusRate: number | null;

  deductTreatmentPlanCommission: boolean;
  deductLabCost: boolean;
  deductTreatmentCost: boolean;
  requireLabApproval: boolean;

  kdvEnabled: boolean;
  kdvRate: number | null;
  kdvAppliedPaymentTypes: string | null;

  extraExpenseEnabled: boolean;
  extraExpenseRate: number | null;

  withholdingTaxEnabled: boolean;
  withholdingTaxRate: number | null;

  jobStartPrices: JobStartPriceRequest[];
  priceRanges: PriceRangeRequest[];
}

// ── Assignments ────────────────────────────────────────────────────────

export interface TemplateAssignment {
  publicId: string;
  doctorId: number;
  doctorName: string;
  templateId: number;
  templateName: string;
  effectiveDate: string;
  expiryDate: string | null;
  isActive: boolean;
  createdAt: string;
}

// ── Targets ────────────────────────────────────────────────────────────

export interface DoctorTarget {
  publicId: string;
  doctorId: number;
  doctorName: string | null;
  branchId: number;
  year: number;
  month: number;
  targetAmount: number;
  createdAt: string;
}

export interface BranchTarget {
  publicId: string;
  branchId: number;
  year: number;
  month: number;
  targetAmount: number;
  createdAt: string;
}

// ── Pending / Distribute / Doctor Account ──────────────────────────────

export interface PendingCommission {
  id: number;
  doctorId: number;
  doctorName: string;
  treatmentPlanItemId: number;
  treatmentId?: number;
  treatmentName?: string;
  branchId: number;
  grossAmount: number;
  netBaseAmount: number;
  commissionRate: number;
  commissionAmount: number;
  netCommissionAmount: number;
  bonusApplied: boolean;
  periodYear: number;
  periodMonth: number;
  createdAt: string;
}

export interface PendingCommissionsSummary {
  items: PendingCommission[];
  totalNet: number;
  count: number;
}

export interface BatchDistributionResult {
  distributed: number;
  skipped: number;
  totalAmount: number;
  warnings: string[];
}

export interface DoctorMonthlyPeriod {
  year: number;
  month: number;
  totalGross: number;
  totalDeductions: number;
  totalCommission: number;
  totalNet: number;
  completedCount: number;
  bonusApplied: boolean;
  targetAmount?: number;
  targetReached: boolean;
}

export interface DoctorAccount {
  doctorId: number;
  doctorName: string;
  branchId?: number;
  totalPending: number;
  totalDistributed: number;
  monthly: DoctorMonthlyPeriod[];
}

export const commissionsApi = {
  // Templates
  listTemplates: (activeOnly = false) =>
    apiClient.get<CommissionTemplate[]>('/commission-templates', { params: { activeOnly } }),

  getTemplate: (publicId: string) =>
    apiClient.get<CommissionTemplate>(`/commission-templates/${publicId}`),

  createTemplate: (body: CommissionTemplateInput) =>
    apiClient.post<CommissionTemplate>('/commission-templates', body),

  updateTemplate: (publicId: string, body: CommissionTemplateInput) =>
    apiClient.put<CommissionTemplate>(`/commission-templates/${publicId}`, body),

  deleteTemplate: (publicId: string) =>
    apiClient.delete(`/commission-templates/${publicId}`),

  // Assignments
  listAssignments: (params?: { doctorId?: number; activeOnly?: boolean }) =>
    apiClient.get<TemplateAssignment[]>('/commission-assignments', { params }),

  assignTemplate: (body: { userPublicId: string; templatePublicId: string; effectiveDate: string; expiryDate?: string }) =>
    apiClient.post<TemplateAssignment>('/commission-assignments', body),

  unassign: (publicId: string) =>
    apiClient.delete(`/commission-assignments/${publicId}`),

  // Targets
  listDoctorTargets: (params?: { doctorId?: number; branchId?: number; year?: number; month?: number }) =>
    apiClient.get<DoctorTarget[]>('/commission-targets/doctors', { params }),

  upsertDoctorTarget: (body: { doctorId: number; branchId: number; year: number; month: number; targetAmount: number }) =>
    apiClient.put<DoctorTarget>('/commission-targets/doctors', body),

  listBranchTargets: (params?: { branchId?: number; year?: number; month?: number }) =>
    apiClient.get<BranchTarget[]>('/commission-targets/branches', { params }),

  upsertBranchTarget: (body: { branchId: number; year: number; month: number; targetAmount: number }) =>
    apiClient.put<BranchTarget>('/commission-targets/branches', body),

  // Pending & distribute
  getPending: (params?: { doctorId?: number; branchId?: number; year?: number; month?: number }) =>
    apiClient.get<PendingCommissionsSummary>('/commissions/pending', { params }),

  calculate: (treatmentPlanItemId: number) =>
    apiClient.post('/commissions/calculate', { treatmentPlanItemId }),

  distributeBatch: (commissionIds: number[]) =>
    apiClient.post<BatchDistributionResult>('/commissions/distribute-batch', { commissionIds }),

  // Doctor account
  getDoctorAccount: (doctorId: number, params?: { branchId?: number; year?: number }) =>
    apiClient.get<DoctorAccount>(`/doctors/${doctorId}/account`, { params }),
};
