import type { LookupItemDto } from "@/shared/models/lazy-data/lookup-item.dto";

export interface LookupResponseDto {
  totalRecords: number;
  data: LookupItemDto[];
}
