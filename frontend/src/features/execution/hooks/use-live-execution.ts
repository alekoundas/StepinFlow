import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ExecutionEventTypeEnum } from "@/shared/enums/backend/execution/execution-event-type-enum";
import type { ExecutionEventDto } from "@/shared/models/execution-event-dto";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";
import { executionKeys, useExecutionState, useLiveExecutionState } from "@/features/execution/hooks/use-execution";
import { useIsExecuting } from "@/features/execution/hooks/use-is-executing";

interface LiveExecution {
  flowId: number;
  executionId: number;
  stepCount: number;

  /** Its row, for when it started. Null until it has been read. */
  execution: ExecutionFeedItemDto | null;
}

/**
 * The execution going now, if one is, with a count of steps that keeps moving. When it ends, the
 * executions page's feed, counts and flow list are read again, since it is in all three.
 */
export function useLiveExecution(): LiveExecution | null {
  const queryClient = useQueryClient();
  const isExecuting = useIsExecuting();
  const { data: engine } = useExecutionState();
  const { data: live } = useLiveExecutionState(isExecuting);

  const executionId = engine?.executionId ?? 0;
  const flowId = engine?.flowId ?? 0;

  // Under the feed's key, so whatever refreshes the feed refreshes this.
  const { data: execution } = useQuery({
    queryKey: [...executionKeys.feeds(), "live", executionId],
    queryFn: async () => {
      const page = await backendApiService.Execution.getFeed({ flowId, beforeId: executionId + 1, statuses: [] });
      return page.find((x) => x.id === executionId) ?? null;
    },
    enabled: isExecuting && executionId > 0,
  });

  useEffect(() => {
    return ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.EXECUTION_EVENT) return;
      if ((message.payload as ExecutionEventDto).type !== ExecutionEventTypeEnum.RUN_ENDED) return;

      queryClient.invalidateQueries({ queryKey: executionKeys.feeds() });
      queryClient.invalidateQueries({ queryKey: executionKeys.allStats() });
      queryClient.invalidateQueries({ queryKey: executionKeys.flowSummaries() });
    });
  }, [queryClient]);

  if (!isExecuting || executionId === 0) return null;

  return {
    flowId,
    executionId,
    stepCount: live?.executionId === executionId ? live.stepCount : (engine?.stepCount ?? 0),
    execution: execution ?? null,
  };
}
