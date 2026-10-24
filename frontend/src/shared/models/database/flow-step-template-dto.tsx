import { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";

// A step's use of a picture: the picture is the flow's and may be shared, accuracy and required
// are the step's.
export class FlowStepTemplateDto {
  id: number = 0;
  orderNumber: number = 0;

  isRequired: boolean = false;

  // Its own bar: one variant of an icon can need a looser one than another.
  accuracy: number = 0.8;

  flowStepId: number = 0;

  // 0 saves flowTemplate as a new picture of the flow.
  flowTemplateId: number = 0;
  flowTemplate: FlowTemplateDto = new FlowTemplateDto();

  constructor(data: Partial<FlowStepTemplateDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
