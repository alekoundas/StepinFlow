import type { TreeNode } from "primereact/treenode";
import { useCallback, useEffect, useState, type ReactNode } from "react";
import {
  Tree,
  type TreeEventNodeEvent,
  type TreeNodeTemplateOptions,
  type TreeSelectionEvent,
} from "primereact/tree";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { DataTreeFlowTemplate } from "@/features/flow/components/data-tree-templates/DataTreeFlowTemplate";
import { backendApiService } from "@/shared/services/backend-api-service";
import { TreeNodeDto } from "@/shared/models/tree-node-dto";
import IconComponent from "@/shared/components/IconComponent";

interface Props<T> {
  flowId: number;
  kek?: T;
}

export function DataTreeComponent<T>({ flowId }: Props<T>) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const {
    selectedTreeNode,
    treeRefreshTrigger,
    setSelectedTreeNode,
    setSelectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
  } = useWorkflowStore();
  const [loading, setLoading] = useState(false);

  // ====================== HELPERS ======================

  const getNewChild = useCallback(
    (flowId?: number, parentFlowStepId?: number): TreeNodeDto =>
      new TreeNodeDto({
        key: crypto.randomUUID().toString(),
        name: "New item",
        isNew: true,
        leaf: true,

        parentFlowId: flowId,
        parentFlowStepId: parentFlowStepId,
      }),
    [],
  );

  /** Recursive immutable update – finds the node by key and replaces its children */
  const updateTreeNodeChildren = (
    nodes: TreeNodeDto[],
    targetKey: string,
    newChildren: TreeNodeDto[],
  ): TreeNodeDto[] => {
    return nodes.map((node) => {
      if (node.key === targetKey) {
        return {
          ...node,
          children: newChildren,
        };
      }
      if (node.children?.length) {
        return {
          ...node,
          children: updateTreeNodeChildren(
            node.children,
            targetKey,
            newChildren,
          ),
        };
      }
      return node;
    });
  };

  /** Recursive node search by key */
  const findNodeByKey = (
    nodes: TreeNodeDto[],
    key: number | string,
  ): TreeNodeDto | undefined => {
    const node = nodes.find((node) => node.key === key);
    if (node) return node;

    if (nodes.flatMap((x) => x.children).length > 0) {
      const found = findNodeByKey(
        nodes.flatMap((x) => x.children),
        key,
      );
      if (found) {
        return found;
      }
    }
    return undefined;
  };

  // ====================== LAZY LOADING ======================

  const loadTreeChildren = useCallback(
    async (
      parentNodeId: number,
      isParentNodeFlow: boolean,
    ): Promise<TreeNodeDto[] | undefined> => {
      setLoading(true);
      try {
        let response =
          await backendApiService.FlowStep.getTreeNodes(parentNodeId);
        if (isParentNodeFlow) {
          response = [...response, getNewChild(parentNodeId, undefined)]; // add + child at the end
        } else {
          response = [...response, getNewChild(undefined, parentNodeId)]; // add + child at the end
        }
        setData((prev) =>
          updateTreeNodeChildren(prev, parentNodeId.toString(), response),
        );
        return response;
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
      return;
    },
    [getNewChild],
  );

  useEffect(() => {
    backendApiService.Flow.getTreeNodes(flowId).then((response) =>
      setData(response),
    );
  }, [flowId]);

  useEffect(() => {
    if (!treeRefreshTrigger) return;

    const { id, isFlow, selectNodeIdAfterLoad } = treeRefreshTrigger;

    loadTreeChildren(id, isFlow).then((response) => {
      if (selectNodeIdAfterLoad) {
        const newSelectedNode = response?.find(
          (x) => x.key === selectNodeIdAfterLoad.toString(),
        );
        setSelectedTreeNode(newSelectedNode);
      }
    });

    setTreeRefreshTrigger(null);
  }, [treeRefreshTrigger, loadTreeChildren]); // loadTreeChildren is stable if you wrap it in useCallback

  const onExpand = async (e: TreeEventNodeEvent) => {
    const node = e.node as TreeNodeDto;
    await loadTreeChildren(+node.key, node.isFlow);
  };

  // ====================== CONTROLLED SELECTION ======================

  const onSelectionChange = (e: TreeSelectionEvent) => {
    setSelectedFlowStepTypeToAdd(undefined);
    const key = e.value; // this can be string | object | null
    if (key == null) {
      setSelectedTreeNode(undefined);
      return;
    }

    // For single selection mode, PrimeReact gives us a string key
    const found = findNodeByKey(data, key as string | number);
    if (found) {
      setSelectedTreeNode(found);
    }
  };

  const nodeTemplate = (
    treeNode: TreeNode,
    _options: TreeNodeTemplateOptions,
  ): ReactNode => {
    const treeNodeDto = treeNode as TreeNodeDto;
    const isSelected = selectedTreeNode?.key === treeNodeDto.key;

    let template: ReactNode;
    if (treeNodeDto.isFlow) {
      template = <DataTreeFlowTemplate treeNode={treeNodeDto} />;
    } else if (treeNodeDto.isNew) {
      template = <IconComponent name="plus" />;
    } else {
      switch (treeNodeDto.flowStepType) {
        case FlowStepTypeEnum.LOOP:
          template = <DataTreeFlowTemplate treeNode={treeNodeDto} />;
          break;

        case FlowStepTypeEnum.WAIT:
          template = <DataTreeFlowTemplate treeNode={treeNodeDto} />;
          break;
      }
    }

    return (
      <div className="flex w-full gap-2 cursor-pointer">
        <div className="flex w-full">{template}</div>
        {isSelected && !treeNodeDto.isNew && <IconComponent name="check" />}
      </div>
    );
  };

  return (
    <Tree
      value={data}
      onExpand={onExpand}
      selectionMode="single"
      selectionKeys={selectedTreeNode?.key ?? null}
      onSelectionChange={onSelectionChange}
      loading={loading}
      nodeTemplate={nodeTemplate}
      pt={{
        // PassThrough
        content: () => ({
          className: "border-none bg-transparent shadow-none",
        }),
      }}
    />
  );
}
