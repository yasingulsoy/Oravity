export enum ToothStatus {
  Healthy             = 1,
  Decayed             = 2,
  Filled              = 3,
  Extracted           = 4,
  Implant             = 5,
  Crown               = 6,
  Bridge              = 7,
  RootCanal           = 8,
  CongenitallyMissing = 9,
  Impacted            = 10,
  Abscess             = 11,
  Fractured           = 12,
  Root                = 13,
}

export interface ToothRecord {
  publicId:     string;
  toothNumber:  string;
  quadrantLabel:string;
  toothType:    string;
  status:       ToothStatus;
  statusLabel:  string;
  surfaces:     string | null;
  notes:        string | null;
  recordedBy:   number;
  recordedAt:   string;
  createdAt:    string;
}

export interface DentalChartResponse {
  patientId:     number;
  teeth:         ToothRecord[];
  totalRecorded: number;
  totalHealthy:  number;
}

export interface ToothHistoryResponse {
  id:             number;
  toothNumber:    string;
  oldStatus:      ToothStatus | null;
  oldStatusLabel: string | null;
  newStatus:      ToothStatus;
  newStatusLabel: string;
  changedBy:      number;
  changedByName:  string;
  changedAt:      string;
  reason:         string | null;
}

export const STATUS_META: Record<ToothStatus, { label: string; fill: string; stroke: string; text: string }> = {
  [ToothStatus.Healthy]:             { label: 'Sağlıklı',        fill: '#ffffff', stroke: '#d1d5db', text: '#6b7280' },
  [ToothStatus.Decayed]:             { label: 'Çürük',           fill: '#fca5a5', stroke: '#ef4444', text: '#991b1b' },
  [ToothStatus.Filled]:              { label: 'Dolgulu',         fill: '#93c5fd', stroke: '#3b82f6', text: '#1e40af' },
  [ToothStatus.Extracted]:           { label: 'Çekilmiş',        fill: '#e5e7eb', stroke: '#9ca3af', text: '#6b7280' },
  [ToothStatus.Implant]:             { label: 'İmplant',         fill: '#c4b5fd', stroke: '#8b5cf6', text: '#4c1d95' },
  [ToothStatus.Crown]:               { label: 'Kron',            fill: '#fde68a', stroke: '#f59e0b', text: '#78350f' },
  [ToothStatus.Bridge]:              { label: 'Köprü',           fill: '#a5f3fc', stroke: '#06b6d4', text: '#164e63' },
  [ToothStatus.RootCanal]:           { label: 'Kanal Tedavili',  fill: '#fdba74', stroke: '#f97316', text: '#7c2d12' },
  [ToothStatus.CongenitallyMissing]: { label: 'Eksik Doğumsal',  fill: '#d1d5db', stroke: '#6b7280', text: '#374151' },
  [ToothStatus.Impacted]:            { label: 'Gömülü',           fill: '#bbf7d0', stroke: '#16a34a', text: '#14532d' },
  [ToothStatus.Abscess]:             { label: 'Apse/Kist',        fill: '#fecdd3', stroke: '#e11d48', text: '#881337' },
  [ToothStatus.Fractured]:           { label: 'Kırık',            fill: '#fed7aa', stroke: '#ea580c', text: '#7c2d12' },
  [ToothStatus.Root]:                { label: 'Kök',              fill: '#e7e5e4', stroke: '#78716c', text: '#44403c' },
};
