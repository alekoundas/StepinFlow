import { useCallback, useMemo, useRef, useState } from "react";

import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { FlowStepMoveDto } from "@/shared/models/flow-step-move.dto";

export type TreeDropPosition = "before" | "inside" | "after";

export interface TreeDropTarget {
  key: string;
  position: TreeDropPosition;
  isValid: boolean;
  /** Why the drop is refused, shown in the drag hint. */
  reason?: string;
}

/** Tells the backend to append. It clamps, so the exact size does not matter. */
const APPEND_INDEX = 2147483647;

/** How long a droppable node must be hovered before it springs open. */
const SPRING_LOAD_MS = 700;

/** Fraction of the row height that means "drop between" rather than "drop inside". */
const EDGE_RATIO = 0.25;

interface IndexEntry {
  node: TreeNodeDto;
  parent: TreeNodeDto | null;
}

interface Props {
  data: TreeNodeDto[];
  flowId: number;
  /** Expand a node that is being hovered, so a step can be dropped into a collapsed branch. */
  onSpringLoad: (node: TreeNodeDto) => void;
  /** Fires once the user has dropped on a valid target. The caller confirms and commits. */
  onDrop: (move: FlowStepMoveDto, dragged: TreeNodeDto) => void;
}

/**
 * Native HTML5 drag and drop for the flow tree.
 *
 * PrimeReact's own dragdropScope only reorders; it cannot express "before / inside / after" or
 * refuse a drop, which is the whole point here. So the rows carry their own handlers and this hook
 * owns the geometry, the validity rules and the translation from a drop into a move request.
 *
 * Validity is checked twice on purpose: cheaply here so the pointer can show the outcome live, and
 * authoritatively on the backend, which sees the branches this tree has not lazily loaded yet.
 */
export function useTreeDragDrop({ data, flowId, onSpringLoad, onDrop }: Props) {
  const [draggedKey, setDraggedKey] = useState<string | null>(null);
  const [dropTarget, setDropTarget] = useState<TreeDropTarget | null>(null);

  const springLoadTimer = useRef<number | null>(null);
  const springLoadKey = useRef<string | null>(null);

  // Flat view of the tree so a row can find its parent and its siblings in one lookup.
  const index = useMemo(() => {
    const map = new Map<string, IndexEntry>();

    const walk = (nodes: TreeNodeDto[], parent: TreeNodeDto | null) => {
      for (const node of nodes) {
        map.set(node.key, { node, parent });
        if (node.children?.length) walk(node.children, node);
      }
    };

    walk(data, null);
    return map;
  }, [data]);

  const draggedNode = draggedKey ? (index.get(draggedKey)?.node ?? null) : null;

  const cancelSpringLoad = useCallback(() => {
    if (springLoadTimer.current !== null) {
      window.clearTimeout(springLoadTimer.current);
      springLoadTimer.current = null;
    }
    springLoadKey.current = null;
  }, []);

  /** Siblings of a node, minus the placeholder row and minus the node being dragged. */
  const getSiblings = useCallback(
    (parent: TreeNodeDto | null, excludeKey: string | null): TreeNodeDto[] => {
      const siblings = parent ? (parent.children ?? []) : data;
      return siblings.filter((x) => !x.isNew && x.key !== excludeKey);
    },
    [data],
  );

  const evaluate = useCallback(
    (target: TreeNodeDto, position: TreeDropPosition): TreeDropTarget => {
      const refuse = (reason: string): TreeDropTarget => ({
        key: target.key,
        position,
        isValid: false,
        reason,
      });

      if (!draggedNode) return refuse("Nothing is being dragged");

      // The placeholder "New item" row is an affordance, not a position.
      if (target.isNew) return refuse("Not a valid position");

      if (target.key === draggedNode.key)
        return refuse("A step cannot be dropped on itself");

      if (position === "inside" && !target.droppable)
        return refuse(`${target.name} cannot contain steps`);

      // The flow root has no siblings, so only "inside" makes sense on it.
      if (position !== "inside" && target.isFlow)
        return refuse("Steps go inside the flow");

      // Before / after lands the step in the target's parent, so that parent has to be able to
      // hold steps. Without this, dropping beside Success or Failure would make the branching
      // step itself the parent, which it can never be.
      if (position !== "inside") {
        const parent = index.get(target.key)?.parent ?? null;

        if (parent && !parent.isFlow && !parent.droppable)
          return refuse(`${parent.name} holds steps in its branches, not directly`);
      }

      // Walk up from the target: dropping into your own subtree detaches it.
      let cursor: TreeNodeDto | null = target;
      while (cursor) {
        if (cursor.key === draggedNode.key)
          return refuse("A step cannot be dropped inside its own children");
        cursor = index.get(cursor.key)?.parent ?? null;
      }

      return { key: target.key, position, isValid: true };
    },
    [draggedNode, index],
  );

  const resolveMove = useCallback(
    (target: TreeNodeDto, position: TreeDropPosition): FlowStepMoveDto | null => {
      if (!draggedNode) return null;

      if (position === "inside") {
        return {
          flowStepId: draggedNode.entityId,
          targetParentFlowStepId: target.isFlow ? undefined : target.entityId,
          targetFlowId: target.isFlow ? target.entityId : undefined,
          targetIndex: APPEND_INDEX,
        };
      }

      const parent = index.get(target.key)?.parent ?? null;

      // Index is computed against the list the backend will renumber: placeholder excluded, and
      // the dragged step already pulled out, so moving a step downwards is not off by one.
      const siblings = getSiblings(parent, draggedNode.key);
      const targetPosition = siblings.findIndex((x) => x.key === target.key);
      if (targetPosition === -1) return null;

      return {
        flowStepId: draggedNode.entityId,
        targetParentFlowStepId:
          parent && !parent.isFlow ? parent.entityId : undefined,
        targetFlowId: !parent || parent.isFlow ? flowId : undefined,
        targetIndex: position === "before" ? targetPosition : targetPosition + 1,
      };
    },
    [draggedNode, flowId, getSiblings, index],
  );

  const getRowDragProps = useCallback(
    (node: TreeNodeDto) => {
      const isDraggable = node.draggable && !node.isNew && !node.isFlow;

      return {
        draggable: isDraggable,

        onDragStart: (event: React.DragEvent) => {
          if (!isDraggable) {
            event.preventDefault();
            return;
          }
          event.dataTransfer.effectAllowed = "move";
          // Firefox refuses to start a drag without payload; the key is enough for us.
          event.dataTransfer.setData("text/plain", node.key);
          setDraggedKey(node.key);
        },

        onDragOver: (event: React.DragEvent) => {
          if (!draggedNode) return;

          // Claiming the event is what makes the row a drop target at all.
          event.preventDefault();
          event.stopPropagation();

          const rect = event.currentTarget.getBoundingClientRect();
          const offset = (event.clientY - rect.top) / (rect.height || 1);

          // A node that can hold children gets three zones, a leaf only two.
          let position: TreeDropPosition;
          if (node.droppable) {
            position =
              offset < EDGE_RATIO
                ? "before"
                : offset > 1 - EDGE_RATIO
                  ? "after"
                  : "inside";
          } else {
            position = offset < 0.5 ? "before" : "after";
          }

          const evaluated = evaluate(node, position);
          event.dataTransfer.dropEffect = evaluated.isValid ? "move" : "none";

          setDropTarget((prev) =>
            prev?.key === evaluated.key &&
            prev.position === evaluated.position &&
            prev.isValid === evaluated.isValid
              ? prev
              : evaluated,
          );

          // Spring-loaded folders: hold over a collapsed branch and it opens for you.
          if (
            evaluated.isValid &&
            position === "inside" &&
            !node.children?.length &&
            springLoadKey.current !== node.key
          ) {
            cancelSpringLoad();
            springLoadKey.current = node.key;
            springLoadTimer.current = window.setTimeout(
              () => onSpringLoad(node),
              SPRING_LOAD_MS,
            );
          } else if (springLoadKey.current !== node.key) {
            cancelSpringLoad();
          }
        },

        onDragLeave: (event: React.DragEvent) => {
          // Ignore the leave that fires when the pointer crosses into a child element.
          if (event.currentTarget.contains(event.relatedTarget as Node)) return;

          cancelSpringLoad();
          setDropTarget((prev) => (prev?.key === node.key ? null : prev));
        },

        onDrop: (event: React.DragEvent) => {
          event.preventDefault();
          event.stopPropagation();
          cancelSpringLoad();

          const current = dropTarget;
          const dragged = draggedNode;

          setDropTarget(null);
          setDraggedKey(null);

          if (!dragged || !current || current.key !== node.key || !current.isValid)
            return;

          const move = resolveMove(node, current.position);
          if (move) onDrop(move, dragged);
        },

        onDragEnd: () => {
          cancelSpringLoad();
          setDropTarget(null);
          setDraggedKey(null);
        },
      };
    },
    [
      cancelSpringLoad,
      draggedNode,
      dropTarget,
      evaluate,
      onDrop,
      onSpringLoad,
      resolveMove,
    ],
  );

  return {
    draggedKey,
    draggedNode,
    dropTarget,
    isDragging: draggedKey !== null,
    getRowDragProps,
  };
}
