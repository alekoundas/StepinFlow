import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ExecutionEventTypeEnum } from "@/shared/enums/backend/execution/execution-event-type-enum";
import type { ExecutionEventDto } from "@/shared/models/execution-event-dto";
import {
  executionKeys,
  useExecutionState,
  useFlowExecutionSummaries,
} from "@/features/execution/hooks/use-execution";

/**
 * The flow last executed, or the one executing now. Read from the same list the executions page
 * reads, which leaves out a flow the branch hid, so the card never offers one that is not there.
 */
export function useLastExecutedFlow() {
  const queryClient = useQueryClient();
  const { data: summaries, isPending } = useFlowExecutionSummaries();
  const { data: engine } = useExecutionState();

  // The engine outlives the page it was started from, so it can finish while Home is open.
  useEffect(() => {
    return ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.EXECUTION_EVENT) return;
      if ((message.payload as ExecutionEventDto).type !== ExecutionEventTypeEnum.RUN_ENDED) return;

      queryClient.invalidateQueries({ queryKey: executionKeys.state() });
      queryClient.invalidateQueries({ queryKey: executionKeys.flowSummaries() });
    });
  }, [queryClient]);

  const flows = summaries ?? [];
  const isExecuting = engine?.isRunning === true;

  const executed = flows
    .filter((x) => x.lastRunOn)
    .sort((a, b) => (a.lastRunOn! < b.lastRunOn! ? 1 : -1));

  let flow = executed.at(0);
  if (isExecuting) flow = flows.find((x) => x.flowId === engine.flowId) ?? flow;

  return { flow, isExecuting, hasFlows: flows.length > 0, isLoading: isPending };
}
