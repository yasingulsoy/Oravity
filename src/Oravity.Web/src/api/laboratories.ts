import apiClient from './client';

// ─── Laboratory ──────────────────────────────────────────────────────────

export interface LaboratoryItem {
  publicId: string;
  name: string;
  code: string | null;
  city: string | null;
  phone: string | null;
  isActive: boolean;
  assignedBranchCount: number;
  assignedBranchNames: string[];
  activeWorkCount: number;
}

export interface LaboratoryResponse {
  publicId: string;
  name: string;
  code: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  country: string | null;
  city: string | null;
  district: string | null;
  address: string | null;
  contactPerson: string | null;
  contactPhone: string | null;
  workingDays: string | null;
  workingHours: string | null;
  paymentTerms: string | null;
  paymentDays: number;
  notes: string | null;
  isActive: boolean;
  assignedBranchCount: number;
  activeWorkCount: number;
  createdAt: string;
}

export interface BranchAssignment {
  publicId: string;
  branchPublicId: string;
  branchName: string;
  priority: number;
  isActive: boolean;
}

export interface LaboratoryPriceItem {
  publicId: string;
  itemName: string;
  itemCode: string | null;
  description: string | null;
  price: number;
  currency: string;
  pricingType: string | null;
  estimatedDeliveryDays: number | null;
  category: string | null;
  validFrom: string | null;
  validUntil: string | null;
  isActive: boolean;
}

export interface LaboratoryDetailResponse {
  laboratory: LaboratoryResponse;
  branchAssignments: BranchAssignment[];
  priceItems: LaboratoryPriceItem[];
}

export interface CreateLaboratoryPayload {
  name: string;
  code?: string | null;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  address?: string | null;
  contactPerson?: string | null;
  contactPhone?: string | null;
  workingDays?: string | null;
  workingHours?: string | null;
  paymentTerms?: string | null;
  paymentDays: number;
  notes?: string | null;
}

export interface UpdateLaboratoryPayload extends CreateLaboratoryPayload {
  isActive: boolean;
}

export interface UpsertPriceItemPayload {
  publicId?: string | null;
  itemName: string;
  itemCode?: string | null;
  description?: string | null;
  price: number;
  currency: string;
  pricingType?: string | null;
  estimatedDeliveryDays?: number | null;
  category?: string | null;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive: boolean;
}

export interface AssignBranchPayload {
  branchPublicId: string;
  priority: number;
  isActive: boolean;
}

// ─── Approval Authority ───────────────────────────────────────────────────

export interface ApprovalAuthority {
  publicId: string;
  userPublicId: string;
  userFullName: string;
  branchPublicId: string | null;
  branchName: string | null;
  canApprove: boolean;
  canReject: boolean;
  notificationEnabled: boolean;
}

export interface UpsertApprovalAuthorityPayload {
  userPublicId: string;
  branchPublicId?: string | null;
  canApprove: boolean;
  canReject: boolean;
  notificationEnabled: boolean;
}

// ─── Laboratory Work ──────────────────────────────────────────────────────

export type LabWorkStatus =
  | 'pending'
  | 'sent'
  | 'in_progress'
  | 'ready'
  | 'received'
  | 'fitted'
  | 'completed'
  | 'approved'
  | 'rejected'
  | 'cancelled';

export interface LaboratoryWorkListItem {
  publicId: string;
  workNo: string;
  patientPublicId: string;
  patientFullName: string;
  doctorPublicId: string;
  doctorFullName: string;
  laboratoryPublicId: string;
  laboratoryName: string;
  branchPublicId: string;
  branchName: string;
  treatmentPlanItemPublicId: string | null;
  workType: string;
  deliveryType: string;
  toothNumbers: string | null;
  shadeColor: string | null;
  status: LabWorkStatus;
  createdAt: string;
  sentToLabAt: string | null;
  estimatedDeliveryDate: string | null;
  receivedFromLabAt: string | null;
  completedAt: string | null;
  totalCost: number | null;
  currency: string | null;
}

export interface LaboratoryWorkItem {
  publicId: string;
  labPriceItemPublicId: string | null;
  itemName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  currency: string;
  notes: string | null;
}

export interface LaboratoryWorkHistoryEntry {
  changedAt: string;
  oldStatus: string | null;
  newStatus: string;
  notes: string | null;
  changedByUserId: number;
}

export interface LaboratoryWorkDetail {
  publicId: string;
  workNo: string;
  patientPublicId: string;
  patientFullName: string;
  doctorPublicId: string;
  doctorFullName: string;
  laboratoryPublicId: string;
  laboratoryName: string;
  branchPublicId: string;
  branchName: string;
  treatmentPlanItemPublicId: string | null;
  workType: string;
  deliveryType: string;
  toothNumbers: string | null;
  shadeColor: string | null;
  status: LabWorkStatus;
  sentToLabAt: string | null;
  estimatedDeliveryDate: string | null;
  receivedFromLabAt: string | null;
  fittedToPatientAt: string | null;
  completedAt: string | null;
  approvedAt: string | null;
  approvedByUserId: number | null;
  totalCost: number | null;
  currency: string | null;
  doctorNotes: string | null;
  labNotes: string | null;
  approvalNotes: string | null;
  attachments: string | null;
  items: LaboratoryWorkItem[];
  history: LaboratoryWorkHistoryEntry[];
  createdAt: string;
}

export interface LaboratoryWorksPage {
  totalCount: number;
  items: LaboratoryWorkListItem[];
}

export interface LabWorkItemInput {
  labPriceItemPublicId?: string | null;
  itemName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  notes?: string | null;
}

export interface CreateLabWorkPayload {
  patientPublicId: string;
  laboratoryPublicId: string;
  treatmentPlanItemPublicId?: string | null;
  branchPublicId?: string | null;
  doctorPublicId?: string | null;
  workType: string;
  deliveryType: string;
  toothNumbers?: string | null;
  shadeColor?: string | null;
  doctorNotes?: string | null;
  items: LabWorkItemInput[];
}

export interface UpdateLabWorkPayload {
  treatmentPlanItemPublicId?: string | null;
  workType: string;
  deliveryType: string;
  toothNumbers?: string | null;
  shadeColor?: string | null;
  doctorNotes?: string | null;
}

export type LabWorkTransitionAction =
  | 'send'
  | 'in_progress'
  | 'ready'
  | 'receive'
  | 'fit'
  | 'complete'
  | 'fast_complete'
  | 'approve'
  | 'reject'
  | 'cancel';

export interface TransitionPayload {
  action: LabWorkTransitionAction;
  notes?: string | null;
}

// ─── API ──────────────────────────────────────────────────────────────────

export const laboratoriesApi = {
  // Labs
  list: (params?: { activeOnly?: boolean; branchPublicId?: string }) =>
    apiClient.get<LaboratoryItem[]>('/laboratories', { params }),

  getDetail: (publicId: string) =>
    apiClient.get<LaboratoryDetailResponse>(`/laboratories/${publicId}`),

  create: (data: CreateLaboratoryPayload) =>
    apiClient.post<LaboratoryResponse>('/laboratories', data),

  update: (publicId: string, data: UpdateLaboratoryPayload) =>
    apiClient.put<LaboratoryResponse>(`/laboratories/${publicId}`, data),

  delete: (publicId: string) =>
    apiClient.delete(`/laboratories/${publicId}`),

  // Branch assignments
  assignBranch: (publicId: string, data: AssignBranchPayload) =>
    apiClient.post<BranchAssignment>(
      `/laboratories/${publicId}/branch-assignments`,
      data,
    ),

  removeBranchAssignment: (assignmentPublicId: string) =>
    apiClient.delete(`/laboratories/branch-assignments/${assignmentPublicId}`),

  // Price items
  upsertPriceItem: (publicId: string, data: UpsertPriceItemPayload) =>
    apiClient.post<LaboratoryPriceItem>(
      `/laboratories/${publicId}/price-items`,
      data,
    ),

  deletePriceItem: (priceItemPublicId: string) =>
    apiClient.delete(`/laboratories/price-items/${priceItemPublicId}`),

  // Approval authorities
  listApprovalAuthorities: () =>
    apiClient.get<ApprovalAuthority[]>('/laboratories/approval-authorities'),

  upsertApprovalAuthority: (data: UpsertApprovalAuthorityPayload) =>
    apiClient.post<ApprovalAuthority>('/laboratories/approval-authorities', data),

  removeApprovalAuthority: (authorityPublicId: string) =>
    apiClient.delete(`/laboratories/approval-authorities/${authorityPublicId}`),

  // Works
  listWorks: (params: {
    status?: string;
    laboratoryPublicId?: string;
    patientPublicId?: string;
    doctorPublicId?: string;
    branchPublicId?: string;
    fromDate?: string;
    toDate?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient.get<LaboratoryWorksPage>('/laboratory-works', {
      params: Object.fromEntries(
        Object.entries(params).filter(([, v]) => v !== undefined && v !== ''),
      ),
    }),

  getWorkDetail: (publicId: string) =>
    apiClient.get<LaboratoryWorkDetail>(`/laboratory-works/${publicId}`),

  createWork: (data: CreateLabWorkPayload) =>
    apiClient.post<LaboratoryWorkDetail>('/laboratory-works', data),

  updateWork: (publicId: string, data: UpdateLabWorkPayload) =>
    apiClient.put<LaboratoryWorkDetail>(`/laboratory-works/${publicId}`, data),

  transitionWork: (publicId: string, data: TransitionPayload) =>
    apiClient.post<LaboratoryWorkDetail>(
      `/laboratory-works/${publicId}/transition`,
      data,
    ),
};
