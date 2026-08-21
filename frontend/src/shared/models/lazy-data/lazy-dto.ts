export interface LazyDto {
  first: number;
  rows: number;
  page: number;
  sortField?: string;
  sortOrder?: 1 | 0 | -1 | null | undefined;
  filters?: Record<string, any>;

  /** Flow.getLazy only: which side of the sub-flow flag to list. */
  isSubFlow?: boolean;
}
