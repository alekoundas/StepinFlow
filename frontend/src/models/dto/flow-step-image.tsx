import type { TemplateMatchModeEnum } from "../enums/template-match-mode-enum";
import type { FlowStep } from "./flow-step";

export interface FlowStepImage {
  id: number;

  // IMAGE_LOCATION_EXTRACT
  templateMatchMode?: TemplateMatchModeEnum;
  templateImage?: string; // byte[] serialized as Base64 string (standard for JSON)
  accuracy: number;
  loopOnMultipleFindings: boolean;

  flowStepId: number;
  flowStep: FlowStep;
}
