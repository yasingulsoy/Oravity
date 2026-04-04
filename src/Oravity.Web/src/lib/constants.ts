export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5010';
export const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL ?? 'http://localhost:5010';

export const STORAGE_KEYS = {
  AUTH: 'oravity-auth',
  UI: 'oravity-ui',
} as const;

export const APPOINTMENT_STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Confirmed: 'bg-green-100 text-green-800',
  InProgress: 'bg-yellow-100 text-yellow-800',
  Completed: 'bg-gray-100 text-gray-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-orange-100 text-orange-800',
};

export const APPOINTMENT_STATUS_LABELS: Record<string, string> = {
  Scheduled: 'Planlandı',
  Confirmed: 'Onaylandı',
  InProgress: 'Devam Ediyor',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal Edildi',
  NoShow: 'Gelmedi',
};
