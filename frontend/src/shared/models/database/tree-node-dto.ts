import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
export class TreeNodeDto {
  key: string = "-1";
  droppable: boolean = false;
  draggable: boolean = false;
  selectable: boolean = true;
  leaf: boolean = false; //Specifies if the node has children. // True doesnt allow expand
  //   className?: string;

  // Custom props
  name: string = "";
  flowStepType?: FlowStepTypeEnum;
  orderNumber: number = -1;
  isFlow: boolean = false;
  isNew: boolean = true;

  parentFlowId?: number;
  parentFlowStepId?: number;
  children: TreeNodeDto[] = [];

  constructor(data: Partial<TreeNodeDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
