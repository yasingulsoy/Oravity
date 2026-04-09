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
}

export enum VisitStatus {
  Waiting = 1,
  ProtocolOpened = 2,
  Completed = 3,
  Cancelled = 4,
}
