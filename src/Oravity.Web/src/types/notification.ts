export interface NotificationItem {
  publicId: string;
  branchId: number;
  toUserId: number | null;
  toRole: number | null;
  type: number;
  typeLabel: string;
  title: string;
  message: string;
  isRead: boolean;
  isUrgent: boolean;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  readAt: string | null;
  createdAt: string;
}

export interface PagedNotificationResult {
  items: NotificationItem[];
  totalCount: number;
  unreadCount: number;
  page: number;
  pageSize: number;
}
