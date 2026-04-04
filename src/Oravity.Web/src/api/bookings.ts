import apiClient from './client';
import type { PagedBookingRequests } from '@/types/booking';

export const bookingsApi = {
  list: (page = 1, pageSize = 20) =>
    apiClient.get<PagedBookingRequests>('/booking-requests', {
      params: { page, pageSize },
    }),

  approve: (id: string) =>
    apiClient.put(`/booking-requests/${id}/approve`),

  reject: (id: string, reason?: string) =>
    apiClient.put(`/booking-requests/${id}/reject`, { reason }),
};
