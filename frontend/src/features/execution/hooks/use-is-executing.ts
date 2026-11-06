import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ExecutionEventTypeEnum } from "@/shared/enums/backend/execution/execution-event-type-enum";
import type { ExecutionEventDto } from "@/shared/models/execution-event-dto";
import { executionKeys, useExecutionState } from "@/features/execution/hooks/use-execution";

/** Whether a flow is executing. The engine outlives the page that started it, so any page can ask. */
export function useIsExecuting(): boolean {
  const queryClient = useQueryClient();
  const { data: engine } = useExecutionState();

  useEffect(() => {
    return ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.EXECUTION_EVENT) return;
      if ((message.payload as ExecutionEventDto).type !== ExecutionEventTypeEnum.RUN_ENDED) return;

      queryClient.invalidateQueries({ queryKey: executionKeys.state() });
    });
  }, [queryClient]);

  return engine?.isRunning === true;
}
