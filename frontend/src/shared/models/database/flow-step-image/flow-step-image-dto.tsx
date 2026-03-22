import type { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";

export class FlowStepImageDto {
  id: number = -1;

  templateMatchMode?: TemplateMatchModeEnum;
  templateImage?: string;
  accuracy: number = 0.8;
  loopOnMultipleFindings: boolean = false;

  flowStepId: number = 0;
  flowStep: FlowStepDto = new FlowStepDto();
}
