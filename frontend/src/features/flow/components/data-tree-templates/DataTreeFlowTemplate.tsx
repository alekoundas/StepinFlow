import LabelComponent from "@/shared/components/LabelComponent";
import type { TreeNodeDto } from "@/shared/models/database/tree-node-dto";

interface Props {
  treeNode: TreeNodeDto;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}
export function DataTreeFlowTemplate({ treeNode }: Props) {
  return (
    <>
      <LabelComponent text={treeNode.orderNumber} />
      <LabelComponent text={treeNode.name} />
    </>
  );
}
