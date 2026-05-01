import apiClient from './client';

// Backend JsonStringEnumConverter ile enumlar string olarak döner.

export type InstitutionInvoiceStatus =
  | 'Issued'
  | 'Paid'
  | 'PartiallyPaid'
  | 'Rejected'
  | 'Overdue'
  | 'InFollowUp'
  | 'Cancelled';

export type InstitutionInvoiceFollowUp =
  | 'None'
  | 'FirstReminder'
  | 'SecondReminder'
  | 'Legal';

export type InstitutionPaymentMethod = 'BankTransfer' | 'Check' | 'Other';

export interface InstitutionInvoice {
  publicId: string;
  id: number;

  patientId: number;
  patientName: string | null;
  patientTcNumber: string | null;

  institutionId: number;
  institutionName: string;
  institutionTaxNumber: string | null;
  institutionTaxOffice: string | null;
  institutionAddress: string | null;
  institutionCity: string | null;

  branchId: number;

  invoiceNo: string;
  invoiceDate: string;
  dueDate: string;

  amount: number;         // Matrah (KDV hariç)
  kdvAmount: number;
  netPayableAmount: number; // Kurumun ödemesi gereken net tutar
  withholdingAmount: number;
  currency: string;

  status: InstitutionInvoiceStatus;
  statusLabel: string;

  paidAmount: number;
  remainingAmount: number;
  paymentDate: string | null;
  paymentReferenceNo: string | null;

  treatmentItemIdsJson: string | null;

  followUpStatus: InstitutionInvoiceFollowUp;
  followUpStatusLabel: string;
  lastFollowUpDate: string | null;
  nextFollowUpDate: string | null;

  notes: string | null;
  createdAt: string;
}

export interface InstitutionPayment {
  publicId: string;
  id: number;
  invoiceId: number;
  patientId: number;
  institutionId: number;
  amount: number;
  currency: string;
  paymentDate: string;
  method: InstitutionPaymentMethod;
  methodLabel: string;
  referenceNo: string | null;
  bankAccountPublicId: string | null;
  notes: string | null;
  isCancelled: boolean;
  createdAt: string;
}

export interface BillableItem {
  id: number;
  treatmentName: string;
  toothNumber: string | null;
  completedAt: string | null;
  institutionAmount: number;
  currency: string;
  planPublicId: string;
  branchId: number;
}

export interface PagedInvoiceResult {
  items: InstitutionInvoice[];
  total: number;
  page: number;
  pageSize: number;
}

export const institutionInvoicesApi = {
  nextNumber: (branchId?: number, type?: string) =>
    apiClient.get<{ number: string; uuid: string | null }>('/institution-invoices/next-number', {
      params: { ...(branchId !== undefined && { branchId }), ...(type && { type }) },
    }),

  getBillableItems: (patientId: number, institutionId: number) =>
    apiClient.get<BillableItem[]>('/institution-invoices/billable-items', {
      params: { patientId, institutionId },
    }),

  list: (params?: {
    status?: InstitutionInvoiceStatus;
    institutionId?: number;
    patientId?: number;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient.get<PagedInvoiceResult>('/institution-invoices', { params }),

  get: (publicId: string) =>
    apiClient.get<InstitutionInvoice>(`/institution-invoices/${publicId}`),

  create: (body: {
    patientId: number;
    institutionId: number;
    invoiceNo: string;
    invoiceDate: string;
    dueDate: string;
    amount: number;
    currency?: string;
    treatmentItemIds?: number[];
    notes?: string;
  }) => apiClient.post<InstitutionInvoice>('/institution-invoices', body),

  registerPayment: (
    publicId: string,
    body: {
      amount: number;
      paymentDate: string;
      method: InstitutionPaymentMethod;
      referenceNo?: string;
      notes?: string;
      currency?: string;
      bankAccountPublicId?: string;
    }
  ) => apiClient.post<InstitutionPayment>(
    `/institution-invoices/${publicId}/payments`, body
  ),

  cancel: (publicId: string, reason: string) =>
    apiClient.post(`/institution-invoices/${publicId}/cancel`, { reason }),

  reject: (publicId: string, reason: string) =>
    apiClient.post(`/institution-invoices/${publicId}/reject`, { reason }),

  followUp: (publicId: string, body: {
    level: InstitutionInvoiceFollowUp;
    onDate: string;
    nextDate?: string;
  }) => apiClient.post(`/institution-invoices/${publicId}/follow-up`, body),

  updateNotes: (publicId: string, notes: string) =>
    apiClient.patch(`/institution-invoices/${publicId}/notes`, { notes }),

  downloadPdf: (publicId: string) =>
    apiClient.get(`/institution-invoices/${publicId}/pdf`, { responseType: 'blob' }),
};
