export class FlowViewportDto {
  id: number = 0;
  width: number = 0;
  height: number = 0;

  /** The order the matrix runs in, taken from the list rather than typed. */
  orderNumber: number = 0;

  flowId: number = 0;

  constructor(data: Partial<FlowViewportDto> = {}) {
    Object.assign(this, { ...data });
  }
}
