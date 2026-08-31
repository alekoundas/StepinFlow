import { useEffect, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
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
          <i
            className={classNames("execution-breakpoint", {
              "execution-breakpoint-on": breakpointStepIds.includes(node.entityId),
            })}
            onClick={(e) => {
              e.stopPropagation();
              toggleBreakpoint(node.entityId);
            }}
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
