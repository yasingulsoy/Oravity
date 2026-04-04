export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  gender: 'Male' | 'Female' | 'Other';
  nationalId?: string;
  address?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PatientListRequest {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  gender: 'Male' | 'Female' | 'Other';
  nationalId?: string;
  address?: string;
  notes?: string;
}

export type UpdatePatientRequest = Partial<CreatePatientRequest>;
