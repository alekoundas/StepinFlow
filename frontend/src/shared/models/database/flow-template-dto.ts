export class FlowTemplateDto {
  // 0 for a picture captured in the form and not saved yet.
  id: number = 0;

  // The file name, given by the backend when the picture is first saved.
  name: string = "";

  // base64 PNG. Left out of list payloads so a save does not push megabytes per step.
  templateImage?: string;

  // Where to click inside the template, in template pixels from its top left.
  clickOffsetX: number = 0;
  clickOffsetY: number = 0;

  // The size of the area it was captured in, and the DPI it was captured at.
  authoredFlowAreaWidth: number = 0;
  authoredFlowAreaHeight: number = 0;
  authoredDpi: number = 0;

  flowId: number = 0;

  // Read only: how many steps link to it.
  flowStepsCount: number = 0;

  constructor(data: Partial<FlowTemplateDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
