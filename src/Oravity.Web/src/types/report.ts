export interface DashboardSummary {
  appointments: AppointmentTodaySummary;
  revenue: RevenueTodaySummary;
  pendingBookingRequests: number;
  unreadNotifications: number;
  generatedAt: string;
}

export interface AppointmentTodaySummary {
  total: number;
  completed: number;
  pending: number;
  noShow: number;
  cancelled: number;
}

export interface RevenueTodaySummary {
  total: number;
  byMethod: RevenueByMethod[];
}

export interface RevenueByMethod {
  method: string;
  amount: number;
  count: number;
}

export interface DailyRevenueReport {
  startDate: string;
  endDate: string;
  grandTotal: number;
  byDay: DailyRevenueLine[];
  byMethod: RevenueByMethod[];
  byDoctor: RevenueByDoctor[];
}

export interface DailyRevenueLine {
  date: string;
  total: number;
  paymentCount: number;
}

export interface RevenueByDoctor {
  doctorId: string;
  doctorName: string;
  total: number;
  paymentCount: number;
}

export interface DoctorPerformanceReport {
  startDate: string;
  endDate: string;
  doctors: DoctorPerformanceLine[];
}

export interface DoctorPerformanceLine {
  doctorId: string;
  doctorName: string;
  completedAppointments: number;
  completedTreatmentItems: number;
  totalRevenue: number;
  totalCommission: number;
  commissionRate: number;
}

export interface AppointmentStatsReport {
  startDate: string;
  endDate: string;
  total: number;
  noShowRate: number;
  avgDurationMinutes: number;
  byStatus: AppointmentStatusSummary[];
  byDay: AppointmentByDayLine[];
}

export interface AppointmentStatusSummary {
  status: number;
  label: string;
  count: number;
  percentage: number;
}

export interface AppointmentByDayLine {
  date: string;
  total: number;
  completed: number;
  noShow: number;
}

export interface PatientStatsReport {
  startDate: string;
  endDate: string;
  newPatients: number;
  totalActivePatients: number;
  topPatients: TopPatientLine[];
}

export interface TopPatientLine {
  patientId: string;
  publicId: string;
  fullName: string;
  treatmentItemCount: number;
  totalPaid: number;
}
