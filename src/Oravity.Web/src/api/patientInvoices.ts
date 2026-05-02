import apiClient from './client';

export type PatientInvoiceStatus = 'Issued' | 'Paid' | 'PartiallyPaid' | 'Cancelled';
export type InvoiceRecipientType = 'IndividualTc' | 'CompanyVkn';

export interface PatientInvoice {
  publicId: string;
  id: number;
  patientId: number;
  patientName: string | null;
  branchId: number;
  invoiceNo: string;
  invoiceType: string;
  invoiceDate: string;
  dueDate: string;
  amount: number;
  kdvRate: number;
  kdvAmount: number;
  totalAmount: number;
  currency: string;
  status: PatientInvoiceStatus;
  statusLabel: string;
  paidAmount: number;
  remainingAmount: number;
  recipientType: InvoiceRecipientType;
  recipientName: string;
  recipientTcNo: string | null;
  recipientVkn: string | null;
  recipientTaxOffice: string | null;
  treatmentItemIdsJson: string | null;
  notes: string | null;
  externalUuid: string | null;
  integratorStatus: string | null;
  createdAt: string;
}

export interface PagedPatientInvoiceResult {
  items: PatientInvoice[];
  total: number;
  page: number;
  pageSize: number;
}

export const patientInvoicesApi = {
  nextNumber: (branchId?: number, type?: string) =>
    apiClient.get<{ number: string; uuid: string | null }>('/patient-invoices/next-number', {
      params: { ...(branchId !== undefined && { branchId }), ...(type && { type }) },
    }),

  list: (params?: {
    status?: PatientInvoiceStatus;
    patientId?: number;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient.get<PagedPatientInvoiceResult>('/patient-invoices', { params }),

  get: (publicId: string) =>
    apiClient.get<PatientInvoice>(`/patient-invoices/${publicId}`),

  create: (body: {
    patientId: number;
    invoiceNo: string;
    invoiceType?: string;
    invoiceDate: string;
    dueDate: string;
    amount: number;
    kdvRate?: number;
    currency?: string;
    recipientType: InvoiceRecipientType;
    recipientName: string;
    recipientTcNo?: string;
    recipientVkn?: string;
    recipientTaxOffice?: string;
    treatmentItemIds?: number[];
    notes?: string;
  }) => apiClient.post<PatientInvoice>('/patient-invoices', body),

  cancel: (publicId: string, reason?: string) =>
    apiClient.post(`/patient-invoices/${publicId}/cancel`, { reason }),

  downloadPdf: (publicId: string) =>
    apiClient.get(`/patient-invoices/${publicId}/pdf`, { responseType: 'blob' }),
};
