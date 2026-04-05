import apiClient from './client';
import type { LookupItem } from '@/types/patient';

export const lookupsApi = {
  getReferralSources: () =>
    apiClient.get<LookupItem[]>('/lookups/referral-sources'),

  getCitizenshipTypes: () =>
    apiClient.get<LookupItem[]>('/lookups/citizenship-types'),
};
