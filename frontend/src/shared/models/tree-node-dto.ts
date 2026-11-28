import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { TreeNodeDetailDto } from "@/shared/models/tree-node-detail-dto";

export class TreeNodeDto {
  key: string = "-1";
  entityId: number = 0;
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
  isDisabled: boolean = false;

  parentFlowId?: number | null;
  parentFlowStepId?: number | null;

  /** What the row shows besides the name. Null for a Flow node. */
  detail?: TreeNodeDetailDto | null;

  children: TreeNodeDto[] = [];

  constructor(data: Partial<TreeNodeDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
