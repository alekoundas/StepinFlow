export type DataTableDto = {
  first: number;
  rows: number;
  page: number;
  sortField?: string;
  sortOrder?: 1 | 0 | -1 | null | undefined;
  filters?: Record<string, any>;
};
