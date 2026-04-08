import apiClient from './client';
import type {
  Appointment,
  AppointmentStatus,
  AppointmentType,
  CalendarSettings,
  Specialization,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  AppointmentCalendarQuery,
  DoctorCalendarInfo,
  AccessibleBranch,
} from '@/types/appointment';

export const appointmentsApi = {
  getByDate: (date: string, branchId?: string, doctorId?: string) =>
    apiClient.get<Appointment[]>('/appointments', {
      params: { date, branchId, doctorId },
    }),

  getCalendar: async (params: AppointmentCalendarQuery) => {
    const start = new Date(params.startDate);
    const end = new Date(params.endDate);
    const dates: string[] = [];
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      dates.push(d.toISOString().split('T')[0]);
    }

    const results = await Promise.all(
      dates.map((date) =>
        apiClient.get<Appointment[]>('/appointments', { params: { date } })
      )
    );

    return { data: results.flatMap((r) => r.data) };
  },

  getById: (id: string) =>
    apiClient.get<Appointment>(`/appointments/${id}`),

  create: (data: CreateAppointmentRequest) =>
    apiClient.post<Appointment>('/appointments', data),

  update: (id: string, data: UpdateAppointmentRequest) =>
    apiClient.put<Appointment>(`/appointments/${id}`, data),

  updateStatus: (publicId: string, statusId: number) =>
    apiClient.put<Appointment>(`/appointments/${publicId}/status`, { statusId }),

  cancel: (publicId: string, reason?: string) =>
    apiClient.delete(`/appointments/${publicId}`, { params: { reason } }),

  getStatuses: () =>
    apiClient.get<AppointmentStatus[]>('/appointments/statuses'),

  getTypes: () =>
    apiClient.get<AppointmentType[]>('/appointments/types'),

  getSpecializations: () =>
    apiClient.get<Specialization[]>('/appointments/specializations'),

  getCalendarDoctors: (params: { date: string; branchIds?: number[]; specializationIds?: number[] }) =>
    apiClient.get<DoctorCalendarInfo[]>('/appointments/calendar-doctors', { params }),

  getAccessibleBranches: () =>
    apiClient.get<AccessibleBranch[]>('/appointments/accessible-branches'),

  getCalendarSettings: () =>
    apiClient.get<CalendarSettings>('/appointments/calendar-settings'),

  updateCalendarSettings: (data: CalendarSettings) =>
    apiClient.put<CalendarSettings>('/appointments/calendar-settings', data),

  getForDoctors: async (date: string, doctorIds: number[]) => {
    if (doctorIds.length === 0) return { data: [] as Appointment[] };
    const results = await Promise.all(
      doctorIds.map((id) =>
        apiClient.get<Appointment[]>('/appointments', { params: { date, doctorId: id } })
      )
    );
    return { data: results.flatMap((r) => r.data) };
  },
};
