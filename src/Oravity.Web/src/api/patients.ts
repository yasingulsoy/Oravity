import apiClient from './client';
import type { Patient, PatientAnamnesis, AnamnesisHistoryItem, PatientListRequest, CreatePatientRequest, UpdatePatientRequest } from '@/types/patient';
import type { PaginatedResponse } from '@/types/common';

export const patientsApi = {
  list: (params: PatientListRequest) =>
    apiClient.get<PaginatedResponse<Patient>>('/patients', {
      params: Object.fromEntries(
        Object.entries(params).filter(([, v]) => v !== undefined && v !== ''),
      ),
    }),

  getById: (id: string) =>
    apiClient.get<Patient>(`/patients/${id}`),

  create: (data: CreatePatientRequest) =>
    apiClient.post<Patient>('/patients', data),

  update: (id: string, data: UpdatePatientRequest) =>
    apiClient.put<Patient>(`/patients/${id}`, data),

  delete: (id: string) =>
    apiClient.delete(`/patients/${id}`),

  getAnamnesis: (publicId: string) =>
    apiClient.get<PatientAnamnesis>(`/patients/${publicId}/anamnesis`),

  upsertAnamnesis: (publicId: string, data: Omit<PatientAnamnesis, 'publicId' | 'hasCriticalAlert' | 'filledAt' | 'filledByName'>) =>
    apiClient.put<PatientAnamnesis>(`/patients/${publicId}/anamnesis`, data),

  getAnamnesisHistory: (publicId: string, limit = 50) =>
    apiClient.get<AnamnesisHistoryItem[]>(`/patients/${publicId}/anamnesis/history`, { params: { limit } }),
};
