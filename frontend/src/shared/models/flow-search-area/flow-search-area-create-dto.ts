export class FlowSearchAreaCreateDto {
  // id: number = -1;
  name: string = "";

  locationLeft: number = -1;
  locationTop: number = -1;
  locationToRight: number = -1;
  locationToBottom: number = -1;

  // Flow
  flowId: number = -1;
  // flow: FlowCreateDto = new FlowCreateDto();

  // flowSteps: FlowStepCreateDto[] = [];

  constructor(data: Partial<FlowSearchAreaCreateDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
