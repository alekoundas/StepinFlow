import type { FlowSearchAreaCreateDto } from "@/shared/models/database/flow-search-area/flow-search-area-create-dto";
// export type IFlowCreateDto = {
//   // ← change to interface
//   name: string;
//   orderNumber: number;
//   flowSearchAreas: FlowSearchAreaCreateDto[];
// };
export class FlowCreateDto {
  // id: number = -1;
  name: string = "";
  orderNumber: number = -1;

  // flowSteps: FlowStepCreateDto[] = [];
  flowSearchAreas: FlowSearchAreaCreateDto[] = [];

  constructor(data: Partial<FlowCreateDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
