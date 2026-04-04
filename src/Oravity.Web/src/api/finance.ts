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

export const financeApi = {
  getSummary: (startDate: string, endDate: string) =>
    apiClient.get<InvoiceSummary>('/finance/summary', {
      params: { startDate, endDate },
    }),

  getInvoices: (params: { page: number; pageSize: number; status?: string }) =>
    apiClient.get<{ items: Invoice[]; totalCount: number }>('/finance/invoices', { params }),
};
