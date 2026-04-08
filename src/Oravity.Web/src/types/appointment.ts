export interface Appointment {
  publicId: string;
  branchId: number;
  patientId: number | null;
  patientName?: string;
  doctorId: number;
  doctorName?: string;
  startTime: string;
  endTime: string;
  statusId: number;
  statusLabel: string;
  notes?: string;
  rowVersion: number;
  createdAt: string;
}

export interface CreateAppointmentRequest {
  patientId: number;
  doctorId: number;
  branchId?: number;
  appointmentTypeId?: number;
  startTime: string;
  endTime: string;
  notes?: string;
}

export interface UpdateAppointmentRequest {
  startTime?: string;
  endTime?: string;
  statusId?: number;
  notes?: string;
}

export interface AppointmentCalendarQuery {
  startDate: string;
  endDate: string;
  doctorId?: number;
  branchId?: number;
}

export interface AppointmentStatus {
  id: number;
  name: string;
  code: string;
  titleColor: string;
  containerColor: string;
  borderColor: string;
  textColor: string;
  className: string;
  isPatientStatus: boolean;
  allowedNextStatusIds: string;
}

export interface AppointmentType {
  id: number;
  name: string;
  code: string;
  color: string;
  isPatientAppointment: boolean;
  defaultDurationMinutes: number;
}

export interface Specialization {
  id: number;
  name: string;
  code: string;
}

export interface DoctorCalendarInfo {
  doctorId: number;
  fullName: string;
  title: string | null;
  calendarColor: string | null;
  specializationId: number | null;
  specializationName: string | null;
  branchId: number;
  branchName: string;
  workStart: string | null;
  workEnd: string | null;
  breakStart: string | null;
  breakEnd: string | null;
  breakLabel: string | null;
  isOnCall: boolean;
}

export interface AccessibleBranch {
  id: number;
  name: string;
}

export interface CalendarSettings {
  slotIntervalMinutes: number;
  dayStartHour: number;
  dayEndHour: number;
}
