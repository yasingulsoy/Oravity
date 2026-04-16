import apiClient from './client';

export interface Campaign {
  publicId: string;
  code: string;
  name: string;
  description: string | null;
  validFrom: string;
  validUntil: string;
  isActive: boolean;
  linkedRulePublicId: string | null;
  createdAt: string;
}

export interface CreateCampaignPayload {
  code: string;
  name: string;
  description?: string | null;
  validFrom: string;
  validUntil: string;
  linkedRulePublicId?: string | null;
}

export interface UpdateCampaignPayload {
  name: string;
  description?: string | null;
  validFrom: string;
  validUntil: string;
  isActive: boolean;
  linkedRulePublicId?: string | null;
}

export const campaignsApi = {
  list: (activeOnly = false) =>
    apiClient.get<Campaign[]>('/campaigns', { params: { activeOnly } }),

  create: (data: CreateCampaignPayload) =>
    apiClient.post<Campaign>('/campaigns', data),

  update: (publicId: string, data: UpdateCampaignPayload) =>
    apiClient.put<Campaign>(`/campaigns/${publicId}`, data),

  delete: (publicId: string) =>
    apiClient.delete(`/campaigns/${publicId}`),
};
