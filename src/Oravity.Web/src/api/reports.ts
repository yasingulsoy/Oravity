import apiClient from './client';
import type {
  DashboardSummary,
  DailyRevenueReport,
  DoctorPerformanceReport,
  AppointmentStatsReport,
  PatientStatsReport,
} from '@/types/report';

export const reportsApi = {
  dashboard: () =>
    apiClient.get<DashboardSummary>('/reports/dashboard'),

  dailyRevenue: (startDate: string, endDate: string, branchId?: string) =>
    apiClient.get<DailyRevenueReport>('/reports/daily-revenue', {
      params: { startDate, endDate, branchId },
    }),

  doctorPerformance: (startDate: string, endDate: string, doctorId?: string) =>
    apiClient.get<DoctorPerformanceReport>('/reports/doctor-performance', {
      params: { startDate, endDate, doctorId },
    }),

  appointmentStats: (startDate: string, endDate: string, branchId?: string) =>
    apiClient.get<AppointmentStatsReport>('/reports/appointments', {
      params: { startDate, endDate, branchId },
    }),

  patientStats: (startDate: string, endDate: string, topCount = 10) =>
    apiClient.get<PatientStatsReport>('/reports/patients', {
      params: { startDate, endDate, topCount },
    }),
};
