import apiClient from './client';
import type { WaitingListItem, DoctorProtocol, ProtocolDetail, IcdCode, ProtocolHistoryItem } from '@/types/visit';

export const visitsApi = {
  getWaitingList: (branchId?: number) =>
    apiClient.get<WaitingListItem[]>('/visits/waiting', {
      params: branchId ? { branchId } : undefined,
    }),

  checkIn: (appointmentPublicId: string) =>
    apiClient.post('/visits/check-in', { appointmentPublicId }),

  checkInWalkIn: (patientId: number, branchId: number, notes?: string) =>
    apiClient.post('/visits/walkin', { patientId, branchId, notes }),

  checkOut: (visitPublicId: string) =>
    apiClient.post(`/visits/${visitPublicId}/checkout`),

  requestCall: (appointmentPublicId: string) =>
    apiClient.post<{ protocolStarted: boolean; patientName: string }>(
      '/visits/request-call',
      { appointmentPublicId },
    ),

  reassignDoctor: (visitPublicId: string, newDoctorId: number) =>
    apiClient.patch(`/visits/${visitPublicId}/reassign-doctor`, { newDoctorId }),
};

export interface ProtocolTypeSetting {
  id: number;
  name: string;
  code: string;
  color: string;
  description: string | null;
}

export const protocolsApi = {
  getMyProtocols: (doctorId?: number) =>
    apiClient.get<DoctorProtocol[]>('/protocols/my', {
      params: doctorId ? { doctorId } : undefined,
    }),

  getTypes: () => apiClient.get<ProtocolTypeSetting[]>('/protocols/types'),

  create: (visitPublicId: string, doctorId: number, protocolType: number) =>
    apiClient.post('/protocols', { visitPublicId, doctorId, protocolType }),

  start: (protocolPublicId: string) =>
    apiClient.post(`/protocols/${protocolPublicId}/start`, {}),

  complete: (protocolPublicId: string, diagnosis?: string, notes?: string) =>
    apiClient.post(`/protocols/${protocolPublicId}/complete`, { diagnosis, notes }),

  getDetail: (publicId: string) =>
    apiClient.get<ProtocolDetail>(`/protocols/${publicId}`),

  updateDetails: (publicId: string, data: {
    chiefComplaint?: string | null;
    examinationFindings?: string | null;
    diagnosis?: string | null;
    treatmentPlan?: string | null;
    notes?: string | null;
  }) => apiClient.put<ProtocolDetail>(`/protocols/${publicId}/details`, data),

  searchIcd: (q?: string, type?: number, limit = 20) =>
    apiClient.get<IcdCode[]>('/protocols/icd/search', { params: { q, type, limit } }),

  addDiagnosis: (publicId: string, icdCodeId: number, isPrimary: boolean, note?: string | null) =>
    apiClient.post(`/protocols/${publicId}/diagnoses`, { icdCodeId, isPrimary, note }),

  removeDiagnosis: (protocolPublicId: string, entryId: string) =>
    apiClient.delete(`/protocols/${protocolPublicId}/diagnoses/${entryId}`),

  getPatientHistory: (patientPublicId: string, limit = 20) =>
    apiClient.get<ProtocolHistoryItem[]>(`/protocols/patient/${patientPublicId}/history`, { params: { limit } }),
};
