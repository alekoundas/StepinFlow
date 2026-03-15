import { TemplateMatchModeEnum } from "../enums/backend/template-match-mode-enum";
import { FlowStep } from "./flow-step";

export interface IFlowStepImage {
  id: number;

  // IMAGE_LOCATION_EXTRACT
  templateMatchMode?: TemplateMatchModeEnum;
  templateImage?: string; // byte[] serialized as Base64 string (standard for JSON)
  accuracy: number;
  loopOnMultipleFindings: boolean;

  flowStepId: number;
  flowStep: FlowStep;
}

export class FlowStepImage implements IFlowStepImage {
  id = -1;

  templateMatchMode?: TemplateMatchModeEnum;
  templateImage?: string;
  accuracy: number = 0.8;
  loopOnMultipleFindings: boolean = false;

  flowStepId: number = 0;
  flowStep: FlowStep = new FlowStep();
}
