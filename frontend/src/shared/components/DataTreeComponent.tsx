import { useEffect, useState, type ReactNode } from "react";
import { Tree } from "primereact/tree";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";
import { backendApiService } from "@/services/backend-api-service";

interface Props<T> {
  flowId: number;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}

export function DataTreeComponent<T>({ flowId }: Props<T>) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const [loading, setLoading] = useState(false);

  const loadTreeChildren = async (id: number, isFlow: boolean) => {
    setLoading(true);
    try {
      let response: TreeNodeDto[];
      if (isFlow) {
        response = await backendApiService.Flow.getTreeNodes(id);
      } else {
        response = await backendApiService.FlowStep.getTreeNodes(id);
      }
      setData(response);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTreeChildren(flowId, true);
  }, []);

  const onExpand = async (e: any) => {
    if (!e.node.children) await loadTreeChildren(e.node.key, false);
  };

  return (
    <Tree
      value={data}
      onExpand={onExpand}
      loading={loading}
    />
  );
}
