import apiClient from './client';

// ── Commission Templates ───────────────────────────────────────────────

export interface JobStartPrice {
  id?: number;
  treatmentId: number;
  treatmentName?: string;
  customPrice: number;
}

export type WorkingStyle = 'Accrual' | 'Collection';
export type PaymentType = 'Fix' | 'Prim' | 'PerJob' | 'PriceRange';
export type JobStartCalculation = 'FromPriceList' | 'CustomPrices';
export type TargetSystem = 'None' | 'Clinic' | 'Doctor';
export type InstitutionPayOnInvoice = 'OnPayment' | 'OnCollection' | 'OnInvoice';

export interface CommissionTemplate {
  id: number;
  publicId: string;
  name: string;
  workingStyle: WorkingStyle;
  workingStyleLabel: string;
  paymentType: PaymentType;
  paymentTypeLabel: string;
  fixedAmount?: number;
  primRate?: number;
  jobStartCalculation: JobStartCalculation;
  jobStartCalculationLabel: string;
  targetSystem: TargetSystem;
  targetSystemLabel: string;
  targetBonusRate?: number;
  institutionPayOnInvoice: InstitutionPayOnInvoice;
  institutionPayOnInvoiceLabel: string;
  deductLabCost: boolean;
  deductTreatmentCost: boolean;
  deductTreatmentPlanCommission: boolean;
  deductCreditCardCommission: boolean;
  applyKdv: boolean;
  kdvRate?: number;
  applyWithholdingTax: boolean;
  withholdingTaxRate?: number;
  extraExpenseAmount?: number;
  notes?: string;
  isActive: boolean;
  jobStartPrices: JobStartPrice[];
  createdAt: string;
}

export interface CommissionTemplateInput {
  name: string;
  workingStyle: WorkingStyle;
  paymentType: PaymentType;
  fixedAmount?: number;
  primRate?: number;
  jobStartCalculation: JobStartCalculation;
  targetSystem: TargetSystem;
  targetBonusRate?: number;
  institutionPayOnInvoice: InstitutionPayOnInvoice;
  deductLabCost: boolean;
  deductTreatmentCost: boolean;
  deductTreatmentPlanCommission: boolean;
  deductCreditCardCommission: boolean;
  applyKdv: boolean;
  kdvRate?: number;
  applyWithholdingTax: boolean;
  withholdingTaxRate?: number;
  extraExpenseAmount?: number;
  notes?: string;
  jobStartPrices: JobStartPrice[];
}

export interface TemplateAssignment {
  id: number;
  publicId: string;
  doctorId: number;
  doctorName: string;
  templateId: number;
  templateName: string;
  startDate: string;
  endDate?: string;
  isActive: boolean;
}

export interface DoctorTarget {
  id: number;
  doctorId: number;
  doctorName?: string;
  branchId: number;
  year: number;
  month: number;
  targetAmount: number;
}

export interface BranchTarget {
  id: number;
  branchId: number;
  year: number;
  month: number;
  targetAmount: number;
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
    apiClient.get<TemplateAssignment[]>('/commission-templates/assignments', { params }),

  assignTemplate: (body: { doctorId: number; templatePublicId: string; startDate: string; endDate?: string }) =>
    apiClient.post<TemplateAssignment>('/commission-templates/assignments', body),

  unassign: (publicId: string) =>
    apiClient.delete(`/commission-templates/assignments/${publicId}`),

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
