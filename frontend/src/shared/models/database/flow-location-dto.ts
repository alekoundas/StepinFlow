export class FlowLocationDto {
  id: number = 0;
  name: string = "";

  // Location in physical (real device) pixels.
  locationX: number = 0;
  locationY: number = 0;

  // Flow
  flowId: number = 0;

  // How many FlowSteps use this location. Read only, set by the backend.
  flowStepsCount: number = 0;

  constructor(data: Partial<FlowLocationDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
