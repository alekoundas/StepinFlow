import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

/**
 * Tree key for an entity id. Must stay in sync with TreeNodeDto.BuildKey on the backend.
 *
 * Flow ids and FlowStep ids are separate sequences, so a raw id would make Flow 5 and FlowStep 5
 * the same node as far as PrimeReact selection and expansion are concerned.
 */
export const buildTreeNodeKey = (id: number, isFlow: boolean): string =>
  isFlow ? `flow-${id}` : `step-${id}`;

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

  parentFlowId?: number;
  parentFlowStepId?: number;
  children: TreeNodeDto[] = [];

  constructor(data: Partial<TreeNodeDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
