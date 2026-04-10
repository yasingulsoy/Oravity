import apiClient from './client';
import type { WaitingListItem } from '@/types/visit';

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
};

export interface ProtocolTypeSetting {
  id: number;
  name: string;
  code: string;
  color: string;
  description: string | null;
}

export const protocolsApi = {
  getTypes: () => apiClient.get<ProtocolTypeSetting[]>('/protocols/types'),

  create: (visitPublicId: string, doctorId: number, protocolType: number) =>
    apiClient.post('/protocols', { visitPublicId, doctorId, protocolType }),

  complete: (protocolPublicId: string, diagnosis?: string, notes?: string) =>
    apiClient.post(`/protocols/${protocolPublicId}/complete`, { diagnosis, notes }),
};
