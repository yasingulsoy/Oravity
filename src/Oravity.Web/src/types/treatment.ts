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
  planId: number;
  treatmentId: number;
  treatmentPublicId: string | null;
  treatmentCode: string | null;
  treatmentName: string | null;
  toothNumber: string | null;
  toothSurfaces: string | null;
  bodyRegionCode: string | null;
  status: TreatmentItemStatus;
  statusLabel: string;
  /** Kural motoru tarafından hesaplanan birim fiyat (indirim öncesi). */
  unitPrice: number;
  /** 0–100 arası indirim oranı. */
  discountRate: number;
  /** İndirim uygulandıktan sonraki birim fiyat. */
  finalPrice: number;
  /** KDV oranı (örn. 10 → %10). */
  kdvRate: number;
  /** KDV tutarı (finalPrice * kdvRate / 100). */
  kdvAmount: number;
  /** KDV dahil toplam (finalPrice + kdvAmount). */
  totalAmount: number;
  doctorId: number | null;
  notes: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface TreatmentPlan {
  publicId: string;
  patientId: number;
  branchId: number;
  doctorId: number;
  name: string;
  status: TreatmentPlanStatus;
  statusLabel: string;
  notes: string | null;
  items: TreatmentPlanItem[];
  createdAt: string;
}
