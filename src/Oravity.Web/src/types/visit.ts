export interface WaitingProtocolItem {
  publicId: string;
  protocolNo: string;
  protocolTypeId: number;
  typeName: string;
  typeColor: string;
  status: number;
  statusName: string;
  doctorName: string;
  diagnosis: string | null;
}

export interface WaitingListItem {
  publicId: string;
  patientId: number;
  patientName: string;
  phone: string | null;
  checkInAt: string;
  isWalkIn: boolean;
  status: VisitStatus;
  statusLabel: string;
  appointmentTime: string | null;
  hasOpenProtocol: boolean;
  waitingMinutes: number;
  appointmentDoctorId: number | null;
  appointmentSpecializationId: number | null;
  patientBirthDate: string | null;
  patientGender: string | null;
  protocols: WaitingProtocolItem[];
}

export enum VisitStatus {
  Waiting = 1,
  ProtocolOpened = 2,
  Completed = 3,
  Cancelled = 4,
}

export interface DoctorProtocol {
  publicId: string;
  protocolNo: string;
  patientId: number;
  patientName: string;
  phone: string | null;
  protocolType: number;
  protocolTypeName: string;
  status: number;
  statusName: string;
  startedAt: string | null;
}
