export class FlowStepTemplateDto {
  id: number = 0;
  name: string = "";
  orderNumber: number = 0;

  // base64 PNG. Left out of list payloads so a save does not push megabytes per step.
  templateImage?: string;

  isRequired: boolean = false;

  // Its own bar: one variant of an icon can need a looser one than another.
  accuracy: number = 0.8;

  // Where to click inside the template, in template pixels from its top left.
  clickOffsetX: number = 0;
  clickOffsetY: number = 0;

  // The size of the area it was captured in, and the DPI it was captured at.
  authoredFlowAreaWidth: number = 0;
  authoredFlowAreaHeight: number = 0;
  authoredDpi: number = 0;

  flowStepId: number = 0;

  constructor(data: Partial<FlowStepTemplateDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
