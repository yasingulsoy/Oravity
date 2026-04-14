export type TreatmentPlanStatus = 0 | 1 | 2 | 3; // Draft=0, Approved=1, Completed=2, Cancelled=3
export type TreatmentItemStatus = 1 | 2 | 3 | 4; // Planned=1, Approved=2, Completed=3, Cancelled=4

export const PLAN_STATUS_LABEL: Record<number, string> = {
  0: 'Taslak',
  1: 'Onaylandı',
  2: 'Tamamlandı',
  3: 'İptal',
};

export const ITEM_STATUS_LABEL: Record<number, string> = {
  1: 'Planlandı',
  2: 'Onaylandı',
  3: 'Tamamlandı',
  4: 'İptal',
};

export interface TreatmentPlanItem {
  publicId: string;
  treatmentPublicId: string | null;
  treatmentCode: string | null;
  treatmentName: string | null;
  toothNumber: string | null;
  status: TreatmentItemStatus;
  statusLabel: string;
  unitPrice: number;
  discountRate: number;
  finalPrice: number;
  notes: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface TreatmentPlan {
  publicId: string;
  name: string;
  status: TreatmentPlanStatus;
  statusLabel: string;
  notes: string | null;
  items: TreatmentPlanItem[];
  createdAt: string;
}
