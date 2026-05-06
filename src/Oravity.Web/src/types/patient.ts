export interface Patient {
  id: number;
  publicId: string;
  branchId: number;
  // Kimlik
  hasTcNumber: boolean;
  hasPassportNo: boolean;
  // Kişisel
  firstName: string;
  lastName: string;
  motherName: string | null;
  fatherName: string | null;
  gender: string | null;
  maritalStatus: string | null;
  nationality: string | null;
  citizenshipTypeId: number | null;
  citizenshipTypeName: string | null;
  occupation: string | null;
  smokingType: string | null;
  pregnancyStatus: number | null;
  birthDate: string | null;
  // İletişim
  phone: string | null;
  homePhone: string | null;
  workPhone: string | null;
  email: string | null;
  // Adres
  country: string | null;
  city: string | null;
  district: string | null;
  address: string | null;
  // Tıbbi
  bloodType: string | null;
  // Geliş / Kurum
  referralSourceId: number | null;
  referralSourceName: string | null;
  referralPerson: string | null;
  agreementInstitutionId: number | null;
  agreementInstitutionName: string | null;
  insuranceInstitutionId: number | null;
  insuranceInstitutionName: string | null;
  // Sistem
  notes: string | null;
  preferredLanguageCode: string;
  smsOptIn: boolean;
  campaignOptIn: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface PatientAnamnesis {
  publicId: string;
  // Genel
  bloodType: string | null;
  isPregnant: boolean;
  isBreastfeeding: boolean;
  // Sistemik
  hasDiabetes: boolean;
  hasHypertension: boolean;
  hasHeartDisease: boolean;
  hasPacemaker: boolean;
  hasAsthma: boolean;
  hasEpilepsy: boolean;
  hasKidneyDisease: boolean;
  hasLiverDisease: boolean;
  hasHiv: boolean;
  hasHepatitisB: boolean;
  hasHepatitisC: boolean;
  otherSystemicDiseases: string | null;
  // Diş hekimliği spesifik
  localAnesthesiaAllergy: boolean;
  localAnesthesiaAllergyNote: string | null;
  bleedingTendency: boolean;
  onAnticoagulant: boolean;
  anticoagulantDrug: string | null;
  bisphosphonateUse: boolean;
  // Alerjiler
  hasPenicillinAllergy: boolean;
  hasAspirinAllergy: boolean;
  hasLatexAllergy: boolean;
  otherAllergies: string | null;
  // Diğer
  previousSurgeries: string | null;
  brushingFrequency: number | null;
  usesFloss: boolean;
  smokingStatus: number | null;
  smokingAmount: string | null;
  alcoholUse: number | null;
  additionalNotes: string | null;
  hasCriticalAlert: boolean;
  filledAt: string;
  filledByName: string;
}

export type NoteType = 1 | 2 | 3 | 4 | 5 | 6;

export interface PatientNote {
  publicId: string;
  type: NoteType;
  typeLabel: string;
  title?: string;
  content: string;
  isPinned: boolean;
  isHidden: boolean;
  isAlert: boolean;
  createdBy: number;
  createdByName: string;
  createdAt: string;
  updatedAt?: string;
}

export interface AnamnesisHistoryItem {
  publicId: string;
  filledAt: string;
  filledByName: string;
  hasCriticalAlert: boolean;
  bloodType: string | null;
  smokingStatus: number | null;
  alcoholUse: number | null;
}

export interface PatientListRequest {
  page: number;
  pageSize: number;
  search?: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  tcHash?: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  branchId?: number;
  tcNumber?: string;
  passportNo?: string;
  motherName?: string;
  fatherName?: string;
  phone?: string;
  homePhone?: string;
  workPhone?: string;
  email?: string;
  birthDate?: string;
  gender?: string;
  maritalStatus?: string;
  nationality?: string;
  citizenshipTypeId?: number;
  occupation?: string;
  address?: string;
  country?: string;
  city?: string;
  district?: string;
  bloodType?: string;
  referralSourceId?: number;
  referralPerson?: string;
  notes?: string;
  preferredLanguageCode?: string;
}

export interface UpdatePatientRequest {
  firstName: string;
  lastName: string;
  tcNumber?: string;
  passportNo?: string;
  motherName?: string;
  fatherName?: string;
  gender?: string;
  maritalStatus?: string;
  nationality?: string;
  citizenshipTypeId?: number;
  occupation?: string;
  smokingType?: string;
  pregnancyStatus?: number;
  birthDate?: string;
  phone?: string;
  homePhone?: string;
  workPhone?: string;
  email?: string;
  country?: string;
  city?: string;
  district?: string;
  address?: string;
  bloodType?: string;
  referralSourceId?: number;
  referralPerson?: string;
  agreementInstitutionId?: number;
  insuranceInstitutionId?: number;
  notes?: string;
  preferredLanguageCode?: string;
  smsOptIn?: boolean;
  campaignOptIn?: boolean;
}

export interface LookupItem {
  id: number;
  publicId: string;
  name: string;
  code: string;
  sortOrder: number;
  isGlobal: boolean;
  isActive: boolean;
}
