export type AppointmentStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export interface Appointment {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  branchId: string;
  treatmentId?: string;
  treatmentName?: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAppointmentRequest {
  patientId: string;
  doctorId: string;
  treatmentId?: string;
  startTime: string;
  endTime: string;
  notes?: string;
}

export interface UpdateAppointmentRequest {
  startTime?: string;
  endTime?: string;
  status?: AppointmentStatus;
  notes?: string;
}

export interface AppointmentCalendarQuery {
  startDate: string;
  endDate: string;
  doctorId?: string;
  branchId?: string;
}
