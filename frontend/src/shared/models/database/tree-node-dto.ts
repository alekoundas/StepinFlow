import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export class TreeNodeDto {
  id: number = -1;
  key: number = -1;
  droppable: boolean = false;
  draggable: boolean = false;
  selectable: boolean = true;
  leaf: boolean = false; //Specifies if the node has children. Used in lazy loading.
  //   className?: string;

  // Custom props
  name: string = "";
  flowStepType: FlowStepTypeEnum = FlowStepTypeEnum.FAILURE;
  orderNumber: number = -1;
  isFlow: boolean = false;

  children: TreeNodeDto[] = [];

  //   constructor(data: Partial<FlowDto> = {}) {
  //     Object.assign(this, {
  //       ...data,
  //     });
  //   }
}
