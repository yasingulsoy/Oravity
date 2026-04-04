export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}

export interface SelectOption {
  label: string;
  value: string;
}

export interface Doctor {
  id: string;
  name: string;
  specialization: string;
}

export interface Branch {
  id: string;
  name: string;
  address: string;
}

export interface Treatment {
  id: string;
  name: string;
  category: string;
  durationMinutes: number;
  price: number;
}
