import apiClient from './client';

export interface InvoiceSummary {
  totalRevenue: number;
  totalPending: number;
  totalPaid: number;
  invoiceCount: number;
}

export interface Invoice {
  id: string;
  patientId: string;
  patientName: string;
  amount: number;
  status: 'Pending' | 'Paid' | 'Overdue' | 'Cancelled';
  dueDate: string;
  paidDate?: string;
  createdAt: string;
}

export interface DoctorCommission {
  id: number;
  doctorId: number;
  doctorName?: string;
  treatmentPlanItemId: number;
  branchId: number;
  grossAmount: number;
  commissionRate: number;
  commissionAmount: number;
  netCommissionAmount?: number;
  status: 'Pending' | 'Distributed' | 'Cancelled';
  statusLabel: string;
  distributedAt?: string;
  createdAt: string;
  periodYear?: number;
  periodMonth?: number;
}

export interface PagedCommissionResult {
  items: DoctorCommission[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalCommissionAmount: number;
}

export const financeApi = {
  getSummary: (startDate: string, endDate: string) =>
    apiClient.get<InvoiceSummary>('/finance/summary', {
      params: { startDate, endDate },
    }),

  getInvoices: (params: { page: number; pageSize: number; status?: string }) =>
    apiClient.get<{ items: Invoice[]; totalCount: number }>('/finance/invoices', { params }),

  getCommissions: (params: {
    doctorId?: number;
    from?: string;
    to?: string;
    status?: 'Pending' | 'Distributed' | 'Cancelled';
    page?: number;
    pageSize?: number;
  }) => apiClient.get<PagedCommissionResult>('/commissions', { params }),
};
