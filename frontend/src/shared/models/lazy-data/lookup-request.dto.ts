import type { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import type { StepResultKindEnum } from "@/shared/enums/backend/step-result-kind-enum";

export interface LookupRequestDto {
  searchText?: string;
  excludedIds?: number[];

  // Scope filters. Lookup.flowPoint uses flowId, Lookup.flowStep walks up from flowStepId.
  flowId?: number;
  flowStepId?: number;

  // Lookup.flowArea only.
  flowAreaType?: FlowAreaTypeEnum;
  onlyRoots?: boolean;

  // Lookup.flowStep only: a cursor step wants a location, a condition wants a value.
  resultKind?: StepResultKindEnum;

  // Lookup.goBack only: where the step sits among its siblings, saved or not.
  orderNumber?: number;
}
