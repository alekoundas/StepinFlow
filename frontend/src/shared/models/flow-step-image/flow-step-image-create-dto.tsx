import type { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";
import { FlowStepCreateDto } from "@/shared/models/flow-step/flow-step-create-dto";

export class FlowStepImageCreateDto {
  id: number = -1;

  templateMatchMode?: TemplateMatchModeEnum;
  templateImage?: string;
  accuracy: number = 0.8;
  loopOnMultipleFindings: boolean = false;

  flowStepId: number = 0;
  flowStep: FlowStepCreateDto = new FlowStepCreateDto();
}
