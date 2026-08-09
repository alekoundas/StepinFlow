import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";

export interface LookupRequestDto {
  searchText?: string;
  excludedIds?: number[];

  // Scope filters. Lookup.flowLocation uses flowId, Lookup.flowStep walks up from flowStepId.
  flowId?: number;
  flowStepId?: number;

  // Lookup.flowSearchArea only.
  flowSearchAreaType?: FlowSearchAreaTypeEnum;
}
