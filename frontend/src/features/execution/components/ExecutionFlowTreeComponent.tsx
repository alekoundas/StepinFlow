import { useEffect, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { classNames } from "primereact/utils";
import type { TreeExpandedKeysType } from "primereact/tree";

import ReadOnlyFlowTreeComponent from "@/shared/components/data-tree/ReadOnlyFlowTreeComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionMutations } from "@/features/execution/hooks/use-execution";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  flowId: number;
}

/**
 * The flow, with a gutter you click to set a breakpoint.
 *
 * Reuses the read-only tree so a step is drawn exactly as it is everywhere else, and only adds the
 * gutter and the "you are here" marker on top of it.
 */
export default function ExecutionFlowTreeComponent({ flowId }: Props) {
  const {
    breakpointStepIds,
    currentFlowStepId,
    runState,
    toggleBreakpoint,
  } = useExecutionStore();

  const { setBreakpointsMutation } = useExecutionMutations();

  // The whole flow at once, not a level at a time: a step you have not expanded to is a step you
  // could not put a breakpoint on.
  const { data: treeNodes } = useQuery({
    queryKey: ["flowStep", "treeNodesRecursive", flowId],
    queryFn: () => backendApiService.FlowStep.getTreeNodesRecursive(flowId),
    enabled: flowId > 0,
  });

  // A breakpoint added mid run has to reach the engine, or it only takes effect next time.
  useEffect(() => {
    if (runState === RunStateEnum.FINISHED) return;

    setBreakpointsMutation.mutate(breakpointStepIds);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [breakpointStepIds]);

  // Everything open: a flow you are debugging is one you want to see all of.
  const expandedKeys = useMemo(() => {
    const keys: TreeExpandedKeysType = {};
    collectKeys(treeNodes ?? [], keys);
    return keys;
  }, [treeNodes]);

  return (
    <ReadOnlyFlowTreeComponent
      value={treeNodes ?? []}
      expandedKeys={expandedKeys}
      nodeTemplate={(node: TreeNodeDto) => (
        <div className="flex align-items-center gap-2">
          <BreakpointDot
            isOn={breakpointStepIds.includes(node.entityId)}
            onToggle={() => toggleBreakpoint(node.entityId)}
          />

          <span
            className={classNames({
              "text-primary font-medium": node.entityId === currentFlowStepId,
            })}
          >
            {node.entityId === currentFlowStepId ? "▸ " : ""}
            {node.name}
          </span>
        </div>
      )}
    />
  );
}

function collectKeys(nodes: TreeNodeDto[], keys: TreeExpandedKeysType): void {
  for (const node of nodes) {
    keys[node.key] = true;
    collectKeys(node.children ?? [], keys);
  }
}

interface BreakpointDotProps {
  isOn: boolean;
  onToggle: () => void;
}

/** An empty ring until you click it, so the whole gutter column is a target. */
function BreakpointDot({ isOn, onToggle }: BreakpointDotProps) {
  const [isHovered, setIsHovered] = useState(false);

  return (
    <i
      onClick={(event) => {
        event.stopPropagation();
        onToggle();
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{
        width: 11,
        height: 11,
        borderRadius: "50%",
        flex: "0 0 auto",
        cursor: "pointer",
        background: isOn ? "var(--red-500)" : undefined,
        border: `1px solid ${
          isOn || isHovered ? "var(--red-500)" : "var(--surface-border)"
        }`,
      }}
    />
  );
}
