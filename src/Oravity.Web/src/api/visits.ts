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

export const protocolsApi = {
  create: (visitPublicId: string, doctorId: number, protocolType: number) =>
    apiClient.post('/protocols', { visitPublicId, doctorId, protocolType }),

  complete: (protocolPublicId: string, diagnosis?: string, notes?: string) =>
    apiClient.post(`/protocols/${protocolPublicId}/complete`, { diagnosis, notes }),
};
