import apiClient from './client';
import type { PagedNotificationResult, NotificationItem } from '@/types/notification';

export const notificationsApi = {
  list: (page = 1, pageSize = 30, unreadOnly?: boolean) =>
    apiClient.get<PagedNotificationResult>('/notifications', {
      params: { page, pageSize, unreadOnly },
    }),

  markRead: (id: string) =>
    apiClient.put<NotificationItem>(`/notifications/${id}/read`),

  sendDoctorMessage: (data: {
    message: string;
    title?: string;
    isUrgent?: boolean;
    relatedEntityType?: string;
    relatedEntityId?: number;
  }) => apiClient.post('/notifications/doctor-message', data),
};
