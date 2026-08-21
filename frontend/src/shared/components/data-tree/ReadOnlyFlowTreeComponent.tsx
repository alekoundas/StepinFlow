import { Tree, type TreeEventNodeEvent } from "primereact/tree";
import type { TreeExpandedKeysType } from "primereact/tree";
import type { TreeNode } from "primereact/treenode";
import type { ReactNode } from "react";

import { FlowStepTreeNodeComponent } from "@/features/flow-step/components/templates/data-tree/FlowStepTreeNodeComponent";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  value: TreeNodeDto[];

  expandedKeys?: TreeExpandedKeysType;
  onToggle?: (keys: TreeExpandedKeysType) => void;
  onExpand?: (node: TreeNodeDto) => void;

  /** Override a row, for previews that draw a placeholder differently. */
  nodeTemplate?: (node: TreeNodeDto) => ReactNode;

  onSelect?: (node: TreeNodeDto) => void;
}

/**
 * A tree that only shows. No drag and drop, no workflow store, no selection written anywhere.
 *
 * Shared by the wizard result panel and the sub-flow preview so both draw a step exactly as the
 * real tree does. Deliberately separate from DataTreeComponent, which owns the refresh triggers
 * and the move machinery a preview must not touch.
 */
export default function ReadOnlyFlowTreeComponent({
  value,
  expandedKeys,
  onToggle,
  onExpand,
  nodeTemplate,
  onSelect,
}: Props) {
  return (
    <Tree
      value={value}
      expandedKeys={expandedKeys}
      onToggle={onToggle ? (e) => onToggle(e.value) : undefined}
      onExpand={onExpand ? (e: TreeEventNodeEvent) => onExpand(e.node as TreeNodeDto) : undefined}
      nodeTemplate={(node: TreeNode) => {
        const treeNode = node as TreeNodeDto;

        return (
          <div
            onClick={onSelect ? () => onSelect(treeNode) : undefined}
            className={onSelect ? "cursor-pointer w-full" : "w-full"}
          >
            {nodeTemplate ? (
              nodeTemplate(treeNode)
            ) : (
              <FlowStepTreeNodeComponent treeNode={treeNode} />
            )}
          </div>
        );
      }}
      className="border-none p-0"
      pt={{
        content: () => ({
          className: "border-none bg-transparent shadow-none",
        }),
      }}
    />
  );
}
