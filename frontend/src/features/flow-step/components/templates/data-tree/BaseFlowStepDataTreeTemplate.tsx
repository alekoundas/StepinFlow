import LabelComponent from "@/shared/components/LabelComponent";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  treeNode: TreeNodeDto;
}
export function BaseFlowStepDataTreeTemplate({ treeNode }: Props) {
  return (
    <>
      <LabelComponent text={treeNode.orderNumber} />.
      <LabelComponent text={treeNode.name} />
    </>
  );
}
