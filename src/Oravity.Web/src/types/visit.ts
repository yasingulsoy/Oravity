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
  isBeingCalled: boolean;
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
  patientPublicId: string;
  patientName: string;
  phone: string | null;
  protocolType: number;
  protocolTypeName: string;
  status: number;
  statusName: string;
  startedAt: string | null;
}

export interface ProtocolDiagnosis {
  publicId: string;
  icdCodeId: number;
  code: string;
  description: string;
  category: string;
  isPrimary: boolean;
  note: string | null;
}

export interface ProtocolDetail {
  publicId: string;
  protocolNo: string;
  protocolType: number;
  protocolTypeName: string;
  status: number;
  statusName: string;
  chiefComplaint: string | null;
  examinationFindings: string | null;
  diagnosis: string | null;
  treatmentPlan: string | null;
  notes: string | null;
  diagnoses: ProtocolDiagnosis[];
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface IcdCode {
  id: number;
  code: string;
  description: string;
  category: string;
  type: number;
}

export interface ProtocolHistoryItem {
  publicId: string;
  protocolNo: string;
  createdAt: string;
  branchName: string;
  protocolType: number;
  protocolTypeName: string;
  status: number;
  statusName: string;
  doctorName: string;
  chiefComplaint: string | null;
  diagnosis: string | null;
}
