import apiClient from './client';
import type { Patient, PatientListRequest, CreatePatientRequest, UpdatePatientRequest } from '@/types/patient';
import type { PaginatedResponse } from '@/types/common';

export const patientsApi = {
  list: (params: PatientListRequest) =>
    apiClient.get<PaginatedResponse<Patient>>('/patients', { params }),

  getById: (id: string) =>
    apiClient.get<Patient>(`/patients/${id}`),

  create: (data: CreatePatientRequest) =>
    apiClient.post<Patient>('/patients', data),

  update: (id: string, data: UpdatePatientRequest) =>
    apiClient.put<Patient>(`/patients/${id}`, data),

  delete: (id: string) =>
    apiClient.delete(`/patients/${id}`),
};
