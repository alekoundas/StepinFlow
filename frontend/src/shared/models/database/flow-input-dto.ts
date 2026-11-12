/** An input: a value the flow's steps type in, written {{name}}. */
export class FlowInputDto {
  id: number = 0;
  name: string = "";

  /** The value, written into the script. Never kept for a secret. */
  defaultValue: string = "";

  isSecret: boolean = false;

  /** Whether a secret has its value saved. The value itself never comes back. */
  hasSecretValue: boolean = false;

  /** A secret's value to save. Empty keeps the saved one. */
  newSecretValue: string = "";

  orderNumber: number = 0;

  flowId: number = 0;

  constructor(data: Partial<FlowInputDto> = {}) {
    Object.assign(this, { ...data });
  }
}
