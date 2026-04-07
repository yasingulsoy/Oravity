export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5010';
export const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL ?? 'http://localhost:5010';

export const STORAGE_KEYS = {
  AUTH: 'oravity-auth',
  UI: 'oravity-ui',
} as const;

// Key: statusId (WellKnownIds: 1=Planned, 2=Confirmed, 3=Arrived, 4=Left, 5=InRoom, 6=Cancelled, 7=Completed, 8=NoShow)
// Takvim sayfası için fallback — asıl veriler /api/appointments/statuses'dan gelir
export const APPOINTMENT_STATUS_COLORS: Record<number, string> = {
  1: 'bg-blue-100 text-blue-800',
  2: 'bg-green-100 text-green-800',
  3: 'bg-teal-100 text-teal-800',
  4: 'bg-purple-100 text-purple-800',
  5: 'bg-yellow-100 text-yellow-800',
  6: 'bg-red-100 text-red-800',
  7: 'bg-gray-100 text-gray-800',
  8: 'bg-orange-100 text-orange-800',
};
