import type { TreeNode } from "primereact/treenode";
import type { TreeExpandedKeysType } from "primereact/tree";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  Tree,
  type TreeEventNodeEvent,
  type TreeNodeTemplateOptions,
  type TreeSelectionEvent,
} from "primereact/tree";
import { classNames } from "primereact/utils";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { DataTreeFlowTemplate } from "@/features/flow/components/data-tree-templates/DataTreeFlowTemplate";
import { backendApiService } from "@/shared/services/backend-api-service";
import { TreeNodeDto, buildTreeNodeKey } from "@/shared/models/tree-node-dto";
import type { FlowStepMoveDto } from "@/shared/models/flow-step-move.dto";
import IconComponent from "@/shared/components/IconComponent";
import { BaseFlowStepDataTreeTemplate } from "@/features/flow-step/components/templates/data-tree/BaseFlowStepDataTreeTemplate";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { TreeMoveConfirmDialogComponent } from "@/shared/components/data-tree/TreeMoveConfirmDialogComponent";
import { useTreeDragDrop } from "@/shared/components/data-tree/use-tree-drag-drop";

interface Props {
  flowId: number;
}

export function DataTreeComponent({ flowId }: Props) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const [expandedKeys, setExpandedKeys] = useState<TreeExpandedKeysType>({});
  const {
    selectedTreeNode,
    treeRefreshTrigger,
    setSelectedTreeNode,
    setSelectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
  } = useWorkflowStore();
  const [loading, setLoading] = useState(false);
  const { openCustom, close } = useDialogStore();

  // ====================== HELPERS ======================

  const getNewChild = useCallback(
    (
      orderNumber: number,
      flowId?: number,
      parentFlowStepId?: number,
    ): TreeNodeDto =>
      new TreeNodeDto({
        key: crypto.randomUUID().toString(),
        name: "New item",
        isNew: true,
        leaf: true,
        draggable: false,

        parentFlowId: flowId,
        parentFlowStepId: parentFlowStepId,
        orderNumber: orderNumber,
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

  /** Flat key -> node lookup, rebuilt whenever the tree changes. */
  const nodesByKey = useMemo(() => {
    const map = new Map<string, TreeNodeDto>();

    const walk = (nodes: TreeNodeDto[]) => {
      for (const node of nodes) {
        map.set(node.key, node);
        if (node.children?.length) walk(node.children);
      }
    };

    walk(data);
    return map;
  }, [data]);

  // ====================== LAZY LOADING ======================

  const loadTreeChildren = useCallback(
    async (
      parentNodeId: number,
      isParentNodeFlow: boolean,
    ): Promise<TreeNodeDto[] | undefined> => {
      setLoading(true);
      try {
        // isFlow decides which column the backend matches the id against: the Flow node wants
        // its root steps, a FlowStep node wants its children.
        let response = await backendApiService.FlowStep.getTreeNodes({
          id: parentNodeId,
          isFlow: isParentNodeFlow,
        });
        const maxOrderNumber = response.reduce(
          (max, node) => (node.orderNumber > max ? node.orderNumber : max),
          0,
        );
        // add step + child at the end
        if (isParentNodeFlow) {
          response = [
            ...response,
            getNewChild(maxOrderNumber + 1, parentNodeId, undefined),
          ];
        } else {
          response = [
            ...response,
            getNewChild(maxOrderNumber + 1, undefined, parentNodeId),
          ];
        }
        setData((prev) =>
          updateTreeNodeChildren(
            prev,
            buildTreeNodeKey(parentNodeId, isParentNodeFlow),
            response,
          ),
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

    if (id === -1) {
      backendApiService.Flow.getTreeNodes(flowId).then((response) =>
        setData(response),
      );
    } else {
      loadTreeChildren(id, isFlow).then((response) => {
        if (selectNodeIdAfterLoad) {
          const newSelectedNode = response?.find(
            (x) => x.entityId === selectNodeIdAfterLoad,
          );
          setSelectedTreeNode(newSelectedNode);
        }
      });
    }
    setTreeRefreshTrigger(null);
  }, [treeRefreshTrigger, loadTreeChildren]); // loadTreeChildren is stable if you wrap it in useCallback

  const onExpand = async (e: TreeEventNodeEvent) => {
    const node = e.node as TreeNodeDto;
    await loadTreeChildren(node.entityId, node.isFlow);
  };

  // ====================== DRAG & DROP ======================

  // Held over a collapsed branch, it opens so the step can be dropped inside it.
  const handleSpringLoad = useCallback(
    (node: TreeNodeDto) => {
      setExpandedKeys((prev) => ({ ...prev, [node.key]: true }));
      loadTreeChildren(node.entityId, node.isFlow);
    },
    [loadTreeChildren],
  );

  // Both ends of a move need reloading: the branch the step left and the one it joined.
  const reloadAfterMove = useCallback(
    async (move: FlowStepMoveDto, dragged: TreeNodeDto) => {
      const parents: { id: number; isFlow: boolean }[] = [];

      const add = (id: number | undefined, isFlow: boolean) => {
        if (id === undefined) return;
        if (parents.some((x) => x.id === id && x.isFlow === isFlow)) return;
        parents.push({ id, isFlow });
      };

      add(dragged.parentFlowStepId, false);
      if (dragged.parentFlowStepId === undefined) add(flowId, true);

      add(move.targetParentFlowStepId, false);
      if (move.targetParentFlowStepId === undefined) add(move.targetFlowId, true);

      for (const parent of parents) {
        await loadTreeChildren(parent.id, parent.isFlow);
      }
    },
    [flowId, loadTreeChildren],
  );

  // The drop asks the backend what the move would do, then confirms it. The preview is what makes
  // the dialog worth showing: it names the steps that would lose their search-result reference.
  const handleDrop = useCallback(
    async (move: FlowStepMoveDto, dragged: TreeNodeDto) => {
      const dialogId = "tree-move-confirm";

      let preview;
      try {
        preview = await backendApiService.FlowStep.getMovePreview(move);
      } catch (err) {
        console.error(err);
        return;
      }

      const commit = async () => {
        try {
          await backendApiService.FlowStep.move(move);
          await reloadAfterMove(move, dragged);
        } catch (err) {
          console.error(err);
        } finally {
          close(dialogId);
        }
      };

      openCustom(
        dialogId,
        <TreeMoveConfirmDialogComponent
          preview={preview}
          onConfirm={commit}
          onCancel={() => close(dialogId)}
        />,
      );
    },
    [close, openCustom, reloadAfterMove],
  );

  const { draggedKey, dropTarget, isDragging, getRowDragProps } =
    useTreeDragDrop({
      data,
      flowId,
      onSpringLoad: handleSpringLoad,
      onDrop: handleDrop,
    });

  // ====================== CONTROLLED SELECTION ======================

  const onSelectionChange = (e: TreeSelectionEvent) => {
    setSelectedFlowStepTypeToAdd(undefined);
    const key = e.value; // this can be string | object | null
    if (key == null) {
      setSelectedTreeNode(undefined);
      return;
    }

    const found = nodesByKey.get(key as string);
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
        default:
          template = <BaseFlowStepDataTreeTemplate treeNode={treeNodeDto} />;
      }
    }

    const isDropTarget = dropTarget?.key === treeNodeDto.key;
    const showEdge = isDropTarget && dropTarget.position !== "inside";
    const showInside = isDropTarget && dropTarget.position === "inside";
    const indicatorColor = dropTarget?.isValid
      ? "var(--primary-color)"
      : "var(--red-500)";

    return (
      <div
        {...getRowDragProps(treeNodeDto)}
        className={classNames("relative flex w-full gap-2 cursor-pointer", {
          "opacity-40": draggedKey === treeNodeDto.key,
        })}
        style={{
          // Outline the row when the step would land inside it.
          outline: showInside ? `2px solid ${indicatorColor}` : undefined,
          outlineOffset: showInside ? "2px" : undefined,
          borderRadius: showInside ? "4px" : undefined,
          backgroundColor:
            showInside && dropTarget?.isValid
              ? "var(--highlight-bg)"
              : undefined,
        }}
        title={!dropTarget?.isValid && isDropTarget ? dropTarget?.reason : undefined}
      >
        {/* A line on the edge the step would slot into. */}
        {showEdge && (
          <span
            style={{
              position: "absolute",
              left: 0,
              right: 0,
              height: 2,
              backgroundColor: indicatorColor,
              [dropTarget.position === "before" ? "top" : "bottom"]: -1,
              pointerEvents: "none",
            }}
          />
        )}

        <div className="flex w-full">{template}</div>
        {isSelected && !treeNodeDto.isNew && <IconComponent name="check" />}
      </div>
    );
  };

  return (
    <Tree
      value={data}
      onExpand={onExpand}
      expandedKeys={expandedKeys}
      onToggle={(e) => setExpandedKeys(e.value)}
      selectionMode="single"
      selectionKeys={selectedTreeNode?.key ?? null}
      onSelectionChange={onSelectionChange}
      loading={loading}
      nodeTemplate={nodeTemplate}
      className={classNames({ "select-none": isDragging })}
      pt={{
        // PassThrough
        content: () => ({
          className: "border-none bg-transparent shadow-none",
        }),
      }}
    />
  );
}
