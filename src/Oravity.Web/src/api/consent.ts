import apiClient from './client';

export interface ConsentFormTemplateSummary {
  publicId: string;
  code: string;
  name: string;
  language: string;
  version: string;
  isActive: boolean;
}

export interface ConsentFormTemplateDetail {
  publicId: string;
  code: string;
  name: string;
  language: string;
  version: string;
  contentHtml: string;
  checkboxesJson: string;
  appliesToAllTreatments: boolean;
  treatmentCategoryIdsJson: string | null;
  showDentalChart: boolean;
  showTreatmentTable: boolean;
  requireDoctorSignature: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface ConsentInstanceResponse {
  publicId: string;
  consentCode: string;
  patientId: number;
  treatmentPlanId: number | null;
  treatmentPlanPublicId: string | null;
  formTemplatePublicId: string;
  formTemplateName: string;
  itemPublicIdsJson: string;
  deliveryMethod: string;
  status: string;
  qrToken: string | null;
  qrTokenExpiresAt: string | null;
  smsToken: string | null;
  smsTokenExpiresAt: string | null;
  signedAt: string | null;
  signerName: string | null;
  createdAt: string;
}

export interface ConsentPublicDto {
  consentCode: string;
  status: string;
  formTemplateName: string;
  formContentHtml: string;
  checkboxesJson: string;
  showDentalChart: boolean;
  showTreatmentTable: boolean;
  requireDoctorSignature: boolean;
  itemPublicIdsJson: string;
  patientId: number;
  patientName: string;
  signedAt: string | null;
  signerName: string | null;
}

export const consentFormsApi = {
  list: (activeOnly?: boolean) =>
    apiClient.get<ConsentFormTemplateSummary[]>('/consent-forms', {
      params: activeOnly ? { activeOnly: true } : undefined,
    }),

  getById: (publicId: string) =>
    apiClient.get<ConsentFormTemplateDetail>(`/consent-forms/${publicId}`),

  create: (data: {
    code: string;
    name: string;
    language: string;
    version: string;
    contentHtml: string;
    checkboxesJson: string;
    appliesToAllTreatments: boolean;
    treatmentCategoryIdsJson?: string | null;
    showDentalChart: boolean;
    showTreatmentTable: boolean;
    requireDoctorSignature: boolean;
  }) => apiClient.post<ConsentFormTemplateDetail>('/consent-forms', data),

  update: (publicId: string, data: {
    name: string;
    language: string;
    version: string;
    contentHtml: string;
    checkboxesJson: string;
    appliesToAllTreatments: boolean;
    treatmentCategoryIdsJson?: string | null;
    showDentalChart: boolean;
    showTreatmentTable: boolean;
    requireDoctorSignature: boolean;
  }) => apiClient.put<ConsentFormTemplateDetail>(`/consent-forms/${publicId}`, data),

  delete: (publicId: string) =>
    apiClient.delete(`/consent-forms/${publicId}`),
};

export const consentInstancesApi = {
  create: (data: {
    treatmentPlanPublicId?: string | null;
    formTemplatePublicId: string;
    itemPublicIds: string[];
    deliveryMethod: string;
    patientPublicId?: string | null;
  }) => apiClient.post<ConsentInstanceResponse>('/consent-instances', data),

  cancel: (instancePublicId: string) =>
    apiClient.put<ConsentInstanceResponse>(`/consent-instances/${instancePublicId}/cancel`),

  getByPlan: (planPublicId: string) =>
    apiClient.get<ConsentInstanceResponse[]>(`/treatment-plans/${planPublicId}/consent-instances`),

  getByPatient: (patientPublicId: string) =>
    apiClient.get<ConsentInstanceResponse[]>(`/patients/${patientPublicId}/consent-instances`),

  downloadPdf: (instancePublicId: string) =>
    apiClient.get(`/consent-instances/${instancePublicId}/pdf`, { responseType: 'blob' }),

  // Public — no auth
  getPublicForm: (token: string) =>
    apiClient.get<ConsentPublicDto>(`/public/consent/${token}`),

  sign: (token: string, data: {
    signerName?: string;
    signatureDataBase64?: string;
    doctorSignatureDataBase64?: string;
    checkboxAnswersJson?: string;
  }) => apiClient.post<{ success: boolean; message: string }>(`/public/consent/${token}/sign`, data),
};
