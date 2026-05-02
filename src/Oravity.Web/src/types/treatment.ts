export type TreatmentPlanStatus  = 'Draft' | 'Approved' | 'Completed' | 'Cancelled';
export type TreatmentItemStatus  = 'Planned' | 'Approved' | 'Completed' | 'Cancelled';

export const PLAN_STATUS_LABEL: Record<TreatmentPlanStatus, string> = {
  Draft:     'Taslak',
  Approved:  'Onaylandı',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal',
};

export const ITEM_STATUS_LABEL: Record<TreatmentItemStatus, string> = {
  Planned:   'Planlandı',
  Approved:  'Onaylandı',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal',
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
  /** Referans fiyat listesindeki ham fiyat (kampanya/kural öncesi). Null → bilinmiyor veya fark yok. */
  listPrice: number | null;
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
  /** Fiyat para birimi (TRY, EUR, USD, …). */
  priceCurrency: string;
  /** Fiyat oluşturulurken kullanılan döviz kuru. */
  priceExchangeRate: number;
  /** TRY bazında hesaplanan nihai fiyat. */
  priceBaseAmount: number;
  doctorId: number | null;
  /** Kalemi gerçekleştiren/gerçekleştirecek hekim adı. Null ise plan hekimi sorumludur. */
  doctorName: string | null;
  /** Kalemi "Onaylandı" statüsüne geçiren kullanıcı adı. */
  approvedByName: string | null;
  /** Kalemin onaylandığı zaman. */
  approvedAt: string | null;
  notes: string | null;
  completedAt: string | null;
  createdAt: string;
  /** Kur kilitleme tipi: 1=Plan anı, 2=Yapıldı anı, 3=Manuel. */
  rateLockType: 1 | 2 | 3;
  /** Yapıldı anında kilitlenen kur (rateLockType=2 ise dolu). */
  rateLockedValue: number | null;
  /** Provizyon kurumunun bu kalem için ödeyeceği tutar. Null = girilmedi. */
  institutionContributionAmount: number | null;
  /** Hastanın ödemesi gereken tutar = priceBaseAmount - institutionContributionAmount. */
  patientAmount: number;
}

export interface TreatmentPlan {
  publicId: string;
  patientId: number;
  branchId: number;
  /** Planın bağlı olduğu şube adı. */
  branchName: string | null;
  doctorId: number;
  /** Planı oluşturan sorumlu hekim adı. */
  doctorName: string | null;
  name: string;
  status: TreatmentPlanStatus;
  statusLabel: string;
  notes: string | null;
  createdAt: string;
  /** Bağlı anlaşmalı kurum ID'si. Null = bireysel hasta. */
  institutionId: number | null;
  /** Anlaşmalı kurum adı. */
  institutionName: string | null;
  /** 1=İndirim, 2=Provizyon. Null = kurum yok. */
  institutionPaymentModel: 1 | 2 | null;
  items: TreatmentPlanItem[];
}
