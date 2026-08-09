import type { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export class FlowStepImageDto {
  id: number = 0;
  name: string = "";
  orderNumber: number = 0;

  // base64 PNG. Left out of list payloads so a save does not push megabytes per step.
  templateImage?: string;

  isRequired: boolean = false;

  // Null means "use the step's setting".
  templateMatchMode?: TemplateMatchModeEnum;
  accuracy?: number;

  // Where to click inside the template, in template pixels from its top left.
  clickOffsetX: number = 0;
  clickOffsetY: number = 0;

  // Frame size the template was captured in. The scaling key.
  authoredFrameWidth: number = 0;
  authoredFrameHeight: number = 0;
  authoredMonitorId: string = "";
  authoredMonitorDpi: number = 0;

  allowMultiScale: boolean = false;
  scaleTolerance: number = 0.15;

  flowStepId: number = 0;

  constructor(data: Partial<FlowStepImageDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
