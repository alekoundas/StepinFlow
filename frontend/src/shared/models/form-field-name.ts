import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";

// Every field the shared form inputs can bind to, in one place so adding a DTO does not mean
// editing each input component.
export type FormFieldName =
  | keyof FlowDto
  | keyof FlowStepDto
  | keyof FlowSearchAreaDto
  | keyof FlowLocationDto;
