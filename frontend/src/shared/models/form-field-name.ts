import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";

// Every field the shared form inputs can bind to, in one place so adding a DTO does not mean
// editing each input component.
export type FormFieldName =
  | keyof FlowDto
  | keyof FlowStepDto
  | keyof FlowAreaDto
  | keyof FlowPointDto;
