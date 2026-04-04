import apiClient from './client';
import type {
  Appointment,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  AppointmentCalendarQuery,
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

  updateStatus: (publicId: string, status: string) =>
    apiClient.put(`/appointments/${publicId}/status`, { status }),

  cancel: (publicId: string, reason?: string) =>
    apiClient.delete(`/appointments/${publicId}`, { params: { reason } }),
};
