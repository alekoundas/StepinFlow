/** An input: a value the flow's steps type in, written {{name}}. */
export class FlowCsvColumnDto {
  id: number = 0;
  name: string = "";

  /** What was typed while recording. Never kept for a secret. */
  defaultValue: string = "";

  isSecret: boolean = false;
  orderNumber: number = 0;

  flowId: number = 0;

  constructor(data: Partial<FlowCsvColumnDto> = {}) {
    Object.assign(this, { ...data });
  }
}
