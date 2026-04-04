export interface BookingRequest {
  id: string;
  publicId: string;
  patientName: string;
  patientPhone: string;
  doctorName: string;
  requestedDate: string;
  requestedTime: string;
  notes: string | null;
  status: BookingRequestStatus;
  createdAt: string;
}

export type BookingRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface PagedBookingRequests {
  items: BookingRequest[];
  totalCount: number;
  page: number;
  pageSize: number;
}
