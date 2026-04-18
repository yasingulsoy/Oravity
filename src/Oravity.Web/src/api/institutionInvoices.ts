import apiClient from './client';

export type InstitutionInvoiceStatus =
  | 'Draft'
  | 'Issued'
  | 'Submitted'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Rejected'
  | 'Cancelled';

export interface InstitutionInvoice {
  id: number;
  publicId: string;
  institutionId: number;
  institutionName: string;
  patientId?: number;
  patientName?: string;
  branchId: number;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: InstitutionInvoiceStatus;
  statusLabel: string;
  submittedAt?: string;
  notes?: string;
  rejectionReason?: string;
  followUpScheduled: boolean;
  followUpDate?: string;
  createdAt: string;
}

export interface InstitutionPayment {
  id: number;
  publicId: string;
  invoiceId: number;
  amount: number;
  paymentDate: string;
  method: string;
  methodLabel: string;
  reference?: string;
  notes?: string;
}

export const institutionInvoicesApi = {
  list: (params?: {
    institutionId?: number;
    branchId?: number;
    status?: InstitutionInvoiceStatus;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient.get<{ items: InstitutionInvoice[]; totalCount: number }>(
      '/institution-invoices',
      { params }
    ),

  get: (publicId: string) =>
    apiClient.get<InstitutionInvoice>(`/institution-invoices/${publicId}`),

  create: (body: {
    institutionId: number;
    patientId?: number;
    invoiceNumber: string;
    invoiceDate: string;
    dueDate?: string;
    totalAmount: number;
    treatmentPlanItemIds?: number[];
    notes?: string;
  }) => apiClient.post<InstitutionInvoice>('/institution-invoices', body),

  registerPayment: (
    publicId: string,
    body: {
      amount: number;
      paymentDate: string;
      method: string;
      reference?: string;
      notes?: string;
    }
  ) => apiClient.post<InstitutionPayment>(
    `/institution-invoices/${publicId}/payments`, body
  ),

  reject: (publicId: string, reason: string) =>
    apiClient.post(`/institution-invoices/${publicId}/reject`, { reason }),

  scheduleFollowUp: (publicId: string, followUpDate: string) =>
    apiClient.post(`/institution-invoices/${publicId}/follow-up`, { followUpDate }),

  updateNotes: (publicId: string, notes: string) =>
    apiClient.patch(`/institution-invoices/${publicId}/notes`, { notes }),
};
