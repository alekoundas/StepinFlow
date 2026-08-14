import type { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";

export interface LookupRequestDto {
  searchText?: string;
  excludedIds?: number[];

  // Scope filters. Lookup.flowPoint uses flowId, Lookup.flowStep walks up from flowStepId.
  flowId?: number;
  flowStepId?: number;

  // Lookup.flowArea only.
  flowAreaType?: FlowAreaTypeEnum;
}
