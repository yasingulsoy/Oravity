import apiClient from './client';

// ─── Weekly Schedule ──────────────────────────────────────────────────────────

export interface DoctorScheduleItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  dayOfWeek: number; // 1=Mon … 7=Sun
  isWorking: boolean;
  startTime: string; // "HH:mm"
  endTime: string;
  breakStart: string | null;
  breakEnd: string | null;
  breakLabel: string | null;
}

export interface UpsertSchedulePayload {
  doctorPublicId: string;
  branchPublicId: string;
  dayOfWeek: number;
  isWorking: boolean;
  startTime: string;
  endTime: string;
  breakStart?: string | null;
  breakEnd?: string | null;
  breakLabel?: string | null;
}

export interface UpdateSchedulePayload {
  isWorking: boolean;
  startTime: string;
  endTime: string;
  breakStart?: string | null;
  breakEnd?: string | null;
  breakLabel?: string | null;
}

// ─── Special Days ─────────────────────────────────────────────────────────────

export type SpecialDayType = 1 | 2 | 3; // 1=ExtraWork, 2=HourChange, 3=DayOff

export interface SpecialDayItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  specificDate: string; // "YYYY-MM-DD"
  type: SpecialDayType;
  startTime: string | null;
  endTime: string | null;
  reason: string | null;
}

export interface UpsertSpecialDayPayload {
  doctorPublicId: string;
  branchPublicId: string;
  specificDate: string;
  type: number;
  startTime?: string | null;
  endTime?: string | null;
  reason?: string | null;
}

export interface UpdateSpecialDayPayload {
  type: number;
  startTime?: string | null;
  endTime?: string | null;
  reason?: string | null;
}

// ─── API ─────────────────────────────────────────────────────────────────────

// ─── Online Schedule ──────────────────────────────────────────────────────────

export interface OnlineScheduleItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  dayOfWeek: number;
  isWorking: boolean;
  startTime: string;
  endTime: string;
  breakStart: string | null;
  breakEnd: string | null;
}

export interface UpsertOnlineSchedulePayload {
  doctorPublicId: string;
  branchPublicId: string;
  dayOfWeek: number;
  isWorking: boolean;
  startTime: string;
  endTime: string;
  breakStart?: string | null;
  breakEnd?: string | null;
}

// ─── Online Booking Settings ──────────────────────────────────────────────────

export interface OnlineBookingSettingsItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  isOnlineVisible: boolean;
  slotDurationMinutes: number;
  autoApprove: boolean;
  maxAdvanceDays: number;
  bookingNote: string | null;
  patientTypeFilter: number; // 0=Herkese, 1=Sadece Yeni, 2=Sadece Mevcut
}

export interface UpsertOnlineBookingSettingsPayload {
  doctorPublicId: string;
  branchPublicId: string;
  isOnlineVisible: boolean;
  slotDurationMinutes: number;
  autoApprove: boolean;
  maxAdvanceDays: number;
  bookingNote?: string | null;
  patientTypeFilter: number;
}

// ─── On-Call Settings ─────────────────────────────────────────────────────────

export interface OnCallSettingsItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
  periodType: number; // 1=Weekly, 2=Monthly, 3=Quarterly, 4=SixMonths
  periodStart: string | null;
  periodEnd: string | null;
}

export interface UpsertOnCallSettingsPayload {
  doctorPublicId: string;
  branchPublicId: string;
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
  periodType: number;
  periodStart?: string | null;
  periodEnd?: string | null;
}

// ─── Online Blocks ────────────────────────────────────────────────────────────

export interface OnlineBlockItem {
  id: number;
  doctorPublicId: string;
  branchPublicId: string;
  branchName: string;
  startDatetime: string; // ISO UTC
  endDatetime: string;
  reason: string | null;
}

export interface CreateOnlineBlockPayload {
  doctorPublicId: string;
  branchPublicId: string;
  startDatetime: string;
  endDatetime: string;
  reason?: string | null;
}

export const doctorSchedulesApi = {
  getSchedules: (doctorPublicId: string) =>
    apiClient.get<DoctorScheduleItem[]>(`/doctor-schedules/${doctorPublicId}`),

  upsertSchedule: (data: UpsertSchedulePayload) =>
    apiClient.post<DoctorScheduleItem>('/doctor-schedules', data),

  updateSchedule: (id: number, data: UpdateSchedulePayload) =>
    apiClient.put<DoctorScheduleItem>(`/doctor-schedules/${id}`, data),

  getSpecialDays: (doctorPublicId: string, branchPublicId?: string, fromDate?: string, toDate?: string) =>
    apiClient.get<SpecialDayItem[]>('/doctor-special-days', {
      params: {
        doctorPublicId,
        ...(branchPublicId && { branchPublicId }),
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate }),
      },
    }),

  createSpecialDay: (data: UpsertSpecialDayPayload) =>
    apiClient.post<SpecialDayItem>('/doctor-special-days', data),

  updateSpecialDay: (id: number, data: UpdateSpecialDayPayload) =>
    apiClient.put<SpecialDayItem>(`/doctor-special-days/${id}`, data),

  deleteSpecialDay: (id: number) =>
    apiClient.delete(`/doctor-special-days/${id}`),

  // Online Program
  getOnlineSchedule: (doctorPublicId: string) =>
    apiClient.get<OnlineScheduleItem[]>(`/doctor-online/schedule/${doctorPublicId}`),
  upsertOnlineSchedule: (data: UpsertOnlineSchedulePayload) =>
    apiClient.post<OnlineScheduleItem>('/doctor-online/schedule', data),

  // Online Booking Settings
  getBookingSettings: (doctorPublicId: string, branchPublicId?: string) =>
    apiClient.get<OnlineBookingSettingsItem[]>(`/doctor-online/booking-settings/${doctorPublicId}`, {
      params: branchPublicId ? { branchPublicId } : undefined,
    }),
  upsertBookingSettings: (data: UpsertOnlineBookingSettingsPayload) =>
    apiClient.post<OnlineBookingSettingsItem>('/doctor-online/booking-settings', data),

  // On-Call Settings
  getOnCallSettings: (doctorPublicId: string, branchPublicId?: string) =>
    apiClient.get<OnCallSettingsItem[]>(`/doctor-online/on-call/${doctorPublicId}`, {
      params: branchPublicId ? { branchPublicId } : undefined,
    }),
  upsertOnCallSettings: (data: UpsertOnCallSettingsPayload) =>
    apiClient.post<OnCallSettingsItem>('/doctor-online/on-call', data),

  // Online Blocks
  getOnlineBlocks: (doctorPublicId: string, branchPublicId?: string) =>
    apiClient.get<OnlineBlockItem[]>(`/doctor-online/blocks/${doctorPublicId}`, {
      params: branchPublicId ? { branchPublicId } : undefined,
    }),
  createOnlineBlock: (data: CreateOnlineBlockPayload) =>
    apiClient.post<OnlineBlockItem>('/doctor-online/blocks', data),
  deleteOnlineBlock: (id: number) =>
    apiClient.delete(`/doctor-online/blocks/${id}`),
};
