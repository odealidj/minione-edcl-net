
export interface PaginationMeta {
  page: number;
  page_size: number;
  total_pages: number;
  total_items: number;
  has_previous: boolean;
  has_next: boolean;
  nextPage: number | null;
  prevPage: number | null;
}

export interface ApiResponse<T> {
  code: number;
  status: string;
  message: string;
  data: T;
  pagination: PaginationMeta | null;
  trace_id: string;
}
