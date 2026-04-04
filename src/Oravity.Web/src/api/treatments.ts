import apiClient from './client';
import type { Treatment } from '@/types/common';
import type { TreatmentPlan } from '@/types/treatment';

export const treatmentsApi = {
  list: () =>
    apiClient.get<Treatment[]>('/treatments'),

  getById: (id: string) =>
    apiClient.get<Treatment>(`/treatments/${id}`),
};

export const treatmentPlansApi = {
  getByPatient: (patientId: string) =>
    apiClient.get<TreatmentPlan[]>(`/patients/${patientId}/treatment-plans`),

  getById: (id: string) =>
    apiClient.get<TreatmentPlan>(`/treatment-plans/${id}`),

  create: (data: { patientId: string; doctorId: string; name: string; notes?: string }) =>
    apiClient.post<TreatmentPlan>('/treatment-plans', data),

  approve: (id: string) =>
    apiClient.put<TreatmentPlan>(`/treatment-plans/${id}/approve`),

  addItem: (planId: string, data: {
    treatmentId: string;
    unitPrice: number;
    discountRate: number;
    toothNumber?: string;
    doctorId?: string;
    notes?: string;
  }) =>
    apiClient.post(`/treatment-plans/${planId}/items`, data),

  completeItem: (planId: string, itemId: string) =>
    apiClient.put(`/treatment-plans/${planId}/items/${itemId}/complete`),

  deleteItem: (planId: string, itemId: string) =>
    apiClient.delete(`/treatment-plans/${planId}/items/${itemId}`),
};
