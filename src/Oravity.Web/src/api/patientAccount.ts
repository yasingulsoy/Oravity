import apiClient from './client';

export interface AllocationDetail {
  allocatedAmount: number;   // TRY
  paymentDate: string;       // DateOnly → "YYYY-MM-DD"
  paymentCurrency: string;
  paymentAmount: number;     // orijinal para birimi
  exchangeRate: number;      // ödeme anındaki kur (TRY ise 1)
  methodLabel: string;
}

export interface PatientAccountItem {
  treatmentPlanItemId: number;
  itemPublicId: string;
  treatmentId: number;
  treatmentName: string;
  toothNumber?: string;
  status: string;
  statusLabel: string;
  priceCurrency: string;
  finalPrice: number;
  totalAmount: number;      // orijinal para birimi
  totalAmountTry: number;   // TRY karşılığı
  patientAmount: number;    // hastanın gerçek borcu (TRY, kurum payı düşülmüş)
  allocatedAmount: number;
  institutionAllocatedAmount: number;
  remainingAmount: number;
  completedAt?: string;
  doctorId?: number;
  doctorName?: string;
  planPublicId: string;
  institutionPaymentModel: 1 | 2 | null;
  institutionContributionAmount: number | null;
  institutionName: string | null;
  allocationDetails: AllocationDetail[];
}

export interface PatientAccountPayment {
  id: number;
  publicId: string;
  amount: number;       // Orijinal para birimi
  currency: string;
  exchangeRate: number;
  baseAmount: number;   // TRY karşılığı
  paymentDate: string;
  createdAt: string;
  method: number;
  methodLabel: string;
  notes?: string;
  allocatedAmount: number;
  unallocatedAmount: number;
  isRefunded: boolean;
}

export interface PatientAccountSummary {
  patientId: number;
  patientName: string;
  items: PatientAccountItem[];
  payments: PatientAccountPayment[];
  totalPlanned: number;       // Planned + Approved (gelecek borç)
  totalCompleted: number;     // Tamamlanan (gerçek borç)
  totalPaid: number;
  totalAllocated: number;
  unallocatedAmount: number;
  totalRemaining: number;     // Kalan borç
  balance: number;            // totalPaid - totalCompleted
  balanceLabel: string;
}

export interface CollectPaymentResult {
  payment: {
    publicId: string;
    amount: number;
    currency: string;
    method: number;
    methodLabel: string;
    paymentDate: string;
  };
  allocations: { allocatedAmount: number }[];
  totalAllocated: number;
  unallocatedAmount: number;
}

export interface AllocationApproval {
  id: number;
  publicId: string;
  paymentId?: number;
  institutionPaymentId?: number;
  patientId: number;
  patientName?: string;
  branchId: number;
  amount: number;
  treatmentPlanItemId?: number;
  status: 'Pending' | 'Approved' | 'Rejected';
  statusLabel: string;
  notes?: string;
  requestedAt: string;
  reviewedAt?: string;
  reviewedByName?: string;
  rejectionReason?: string;
}

export const patientAccountApi = {
  getAccount: (patientId: number) =>
    apiClient.get<PatientAccountSummary>(`/patients/${patientId}/account`),

  collectPayment: (patientId: number, body: {
    amount: number;
    method: number;
    paymentDate: string;
    currency?: string;
    exchangeRate?: number;
    notes?: string;
    posTerminalId?: string;
    bankAccountId?: string;
  }) => apiClient.post<CollectPaymentResult>(`/patients/${patientId}/collect-payment`, body),

  allocatePayment: (paymentPublicId: string, allocations: { treatmentPlanItemId: number; amount: number }[]) =>
    apiClient.post(`/payments/${paymentPublicId}/allocate`, { allocations }),

  createPayment: (body: {
    patientId: number;
    amount: number;
    method: number;
    paymentDate: string;
    currency?: string;
    exchangeRate?: number;
    notes?: string;
    posTerminalId?: string;
    bankAccountId?: string;
  }) => apiClient.post<{
    publicId: string;
    id: number;
    amount: number;
    currency: string;
    exchangeRate: number;
    baseAmount: number;
    method: number;
    methodLabel: string;
    paymentDate: string;
    notes?: string;
    isRefunded: boolean;
  }>('/payments', body),

  requestManualAllocation: (body: {
    paymentId?: number;
    institutionPaymentId?: number;
    treatmentPlanItemId?: number;
    amount: number;
    notes?: string;
  }) => apiClient.post<AllocationApproval>('/allocations/request', body),

  listApprovals: (params?: { status?: string; patientId?: number }) =>
    apiClient.get<AllocationApproval[]>('/allocations/approvals', { params }),

  approveAllocation: (publicId: string) =>
    apiClient.post(`/allocations/approvals/${publicId}/approve`),

  rejectAllocation: (publicId: string, reason?: string) =>
    apiClient.post(`/allocations/approvals/${publicId}/reject`, { reason }),

  refundPayment: (publicId: string, reason?: string) =>
    apiClient.post(`/payments/${publicId}/refund`, { reason }),
};
