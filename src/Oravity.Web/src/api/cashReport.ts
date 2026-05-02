import apiClient from './client';

export type CashReportStatus = 'Open' | 'Closed' | 'Approved';

export interface CashReportState {
  publicId: string;
  branchId: number;
  reportDate: string;
  status: number;
  statusLabel: string;
  closedByUserId?: number;
  closedAt?: string;
  closingNotes?: string;
  approvedByUserId?: number;
  approvedAt?: string;
  approvalNotes?: string;
  reopenCount: number;
}

export interface CashPaymentLine {
  publicId: string;
  id: number;
  createdAt: string;
  patientName: string;
  amount: number;
  currency: string;
  exchangeRate: number;
  baseAmount: number;
  method: number;
  methodLabel: string;
  notes?: string;
  recordedByName: string;
}

export interface CashCurrencyTotal {
  currency: string;
  amount: number;
  baseTry: number;
  count: number;
}

export interface CashMethodTotal {
  method: number;
  methodLabel: string;
  totalTry: number;
  count: number;
  byCurrency: CashCurrencyTotal[];
}

export interface PosTotalLine {
  posTerminalPublicId?: string;
  terminalName: string;
  bankName: string;
  totalTry: number;
  count: number;
  byCurrency: CashCurrencyTotal[];
}

export interface BankTotalLine {
  bankAccountPublicId?: string;
  accountName: string;
  bankName: string;
  accountCurrency: string;
  totalTry: number;
  count: number;
  byCurrency: CashCurrencyTotal[];
}

export interface KasaSection {
  oncekiGunDevir: CashCurrencyTotal[];
  bugunNakit: CashCurrencyTotal[];
  toplamKasa: CashCurrencyTotal[];
}

export interface DailyCashReportDetail {
  date: string;
  branchId: number;
  reportStatus: CashReportState | null;
  payments: CashPaymentLine[];
  byMethod: CashMethodTotal[];
  byCurrency: CashCurrencyTotal[];
  totalTry: number;
  totalCount: number;
  posTotals: PosTotalLine[];
  bankTotals: BankTotalLine[];
  kasa: KasaSection;
}

export const cashReportApi = {
  getDetail: (date: string, branchId?: number) =>
    apiClient.get<DailyCashReportDetail>('/cash-reports/detail', {
      params: { date, branchId },
    }),

  close: (date: string, notes?: string) =>
    apiClient.post<CashReportState>('/cash-reports/close', { date, notes }),

  approve: (publicId: string, notes?: string) =>
    apiClient.post<CashReportState>(`/cash-reports/${publicId}/approve`, { notes }),

  reopen: (publicId: string) =>
    apiClient.post<CashReportState>(`/cash-reports/${publicId}/reopen`),
};
