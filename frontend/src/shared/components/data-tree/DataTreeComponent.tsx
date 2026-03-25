import { useEffect, useState, type ReactNode } from "react";
import {
  Tree,
  type TreeEventNodeEvent,
  type TreeNodeTemplateOptions,
  type TreeSelectionEvent,
} from "primereact/tree";
import { backendApiService } from "@/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { DataTreeFlowTemplate } from "@/features/flow/components/data-tree-templates/DataTreeFlowTemplate";
import IconComponent from "@/components/core/icon-component";
import type { TreeNode } from "primereact/treenode";

interface Props<T> {
  flowId: number;
}

export function DataTreeComponent<T>({ flowId }: Props<T>) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const { selectedTreeNode, setSelectedTreeNode } = useWorkflowStore();
  const [loading, setLoading] = useState(false);

  // ====================== HELPERS ======================

  /** Adds the "New item" bubble to every SUB_FLOW node (fixed spread + null-safety) */
  const insertNewBubble = (treeNodes: TreeNodeDto[]): TreeNodeDto[] => {
    const newChild: TreeNodeDto = new TreeNodeDto({
      key: -1,
      name: "New item",
      isNew: true,
      leaf: true,
    });

    return treeNodes.map((item) =>
      item.flowStepType === FlowStepTypeEnum.SUB_FLOW
        ? {
            ...item,
            children: [...(item.children ?? []), newChild],
          }
        : { ...item },
    );
  };

  /** Recursive immutable update – finds the node by key and attaches its children */
  const updateTreeWithChildren = (
    nodes: TreeNodeDto[],
    targetKey: number,
    newChildren: TreeNodeDto[],
  ): TreeNodeDto[] => {
    return nodes.map((node) => {
      if (node.key === targetKey) {
        return {
          ...node,
          children: insertNewBubble(newChildren), 
        };
      }
      if (node.children?.length) {
        return {
          ...node,
          children: updateTreeWithChildren(
            node.children,
            targetKey,
            newChildren,
          ),
        };
      }
      return node;
    });
  };

  /** Finds a node by key anywhere in the tree (used for controlled selection) */
  const findNodeByKey = (
    nodes: TreeNodeDto[],
    key: number | string,
  ): TreeNodeDto | undefined => {
    for (const node of nodes) {
      if (Number(node.key) === Number(key)) return node;
      if (node.children?.length) {
        const found = findNodeByKey(node.children, key);
        if (found) return found;
      }
    }
    return undefined;
  };

  // ====================== LAZY LOADING ======================

  const loadTreeChildren = async (id: number, isFlow: boolean) => {
    setLoading(true);
    try {
      if (isFlow) {
        const response = await backendApiService.Flow.getTreeNodes(id);
        setData(insertNewBubble(response));
      } else {
        // ← THIS WAS MISSING
        const response = await backendApiService.FlowStep.getTreeNodes(id);
        setData((prev) => updateTreeWithChildren(prev, id, response));
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTreeChildren(flowId, true);
  }, [flowId]);

  const onExpand = async (e: TreeEventNodeEvent) => {
    // Only load if the node has no children yet (SUB_FLOW already has the "New item")
    if (!e.node.children) {
      await loadTreeChildren(e.node.key ? +e.node.key : -1, false);
    }
  };

  // ====================== CONTROLLED SELECTION ======================

  const onSelectionChange = (e: TreeSelectionEvent) => {
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
    treeNode: TreeNode, //TreeNodeDto,
    _options: TreeNodeTemplateOptions, // TreeNodeTemplateOptions
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
        // add other cases here if needed
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
      selectionKeys={selectedTreeNode?.key ?? null} // {/* ← controlled */}
      onSelectionChange={onSelectionChange} //{/* ← required for controlled mode */}
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
