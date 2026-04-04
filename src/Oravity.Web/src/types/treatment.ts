export type TreatmentPlanStatus = 'Draft' | 'Approved' | 'Completed';

export interface TreatmentPlanItem {
  publicId: string;
  treatmentName: string;
  toothNumber: string | null;
  unitPrice: number;
  discountRate: number;
  netPrice: number;
  status: string;
  doctorName: string | null;
  completedAt: string | null;
  notes: string | null;
}

export interface TreatmentPlan {
  publicId: string;
  patientId: string;
  patientName: string;
  doctorName: string;
  name: string;
  status: TreatmentPlanStatus;
  totalAmount: number;
  notes: string | null;
  items: TreatmentPlanItem[];
  createdAt: string;
}
