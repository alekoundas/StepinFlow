import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

// export interface TreeNodeDto extends TreeNode {
// //   id: number ;
//   key: number;
//   droppable: boolean ;
//   draggable: boolean ;
//   selectable: boolean;
//   leaf: boolean ; //Specifies if the node has children. Used in lazy loading.
//   //   className?: string;

//   // Custom props
//   name: string
//   flowStepType: FlowStepTypeEnum ;
//   orderNumber: number ;
//   isFlow: boolean ;

//   children: TreeNodeDto[] = [];

//   //   constructor(data: Partial<FlowDto> = {}) {
//   //     Object.assign(this, {
//   //       ...data,
//   //     });
//   //   }
// }

export class TreeNodeDto {
  //   id: number = -1;
  key: number = -1;
  droppable: boolean = false;
  draggable: boolean = false;
  selectable: boolean = true;
  leaf: boolean = false; //Specifies if the node has children. // True doesnt allow expand
  //   className?: string;

  // Custom props
  name: string = "";
  flowStepType: FlowStepTypeEnum = FlowStepTypeEnum.FAILURE;
  orderNumber: number = -1;
  isFlow: boolean = false;

  children: TreeNodeDto[] = [];

  constructor(data: Partial<TreeNodeDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
