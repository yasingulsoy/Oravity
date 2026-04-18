import apiClient from './client';

export interface PatientAccountItem {
  treatmentPlanItemId: number;
  treatmentId: number;
  treatmentName: string;
  tooth?: string;
  status: string;
  statusLabel: string;
  plannedPrice: number;
  finalPrice: number;
  completedAt?: string;
  doctorId?: number;
  doctorName?: string;
}

export interface PatientAccountPayment {
  id: number;
  publicId: string;
  amount: number;
  paymentDate: string;
  method: string;
  methodLabel: string;
  notes?: string;
  allocatedTotal: number;
  unallocatedAmount: number;
}

export interface PatientAccountSummary {
  patientId: number;
  patientName: string;
  items: PatientAccountItem[];
  payments: PatientAccountPayment[];
  totalPlanned: number;
  totalCompleted: number;
  totalPaid: number;
  totalAllocated: number;
  totalUnallocated: number;
  balance: number;
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
};
