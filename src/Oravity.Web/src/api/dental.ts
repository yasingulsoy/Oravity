import apiClient from './client';
import type { DentalChartResponse, ToothRecord, ToothHistoryResponse } from '@/types/dental';
import type { ToothStatus } from '@/types/dental';

export const dentalApi = {
  getChart: (patientPublicId: string) =>
    apiClient.get<DentalChartResponse>(`/patients/${patientPublicId}/dental-chart`),

  updateTooth: (
    patientPublicId: string,
    toothNumber: string,
    status: ToothStatus,
    surfaces?: string | null,
    notes?: string | null,
  ) =>
    apiClient.put<ToothRecord>(
      `/patients/${patientPublicId}/dental-chart/teeth/${toothNumber}`,
      { status, surfaces: surfaces ?? null, notes: notes ?? null },
    ),

  getHistory: (patientPublicId: string, toothNumber: string) =>
    apiClient.get<ToothHistoryResponse[]>(
      `/patients/${patientPublicId}/dental-chart/teeth/${toothNumber}/history`,
    ),
};
