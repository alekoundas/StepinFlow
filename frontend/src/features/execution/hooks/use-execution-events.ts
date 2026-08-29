import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ExecutionEventTypeEnum } from "@/shared/enums/backend/execution/execution-event-type-enum";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { executionKeys } from "@/features/execution/hooks/use-execution";
import type { ExecutionEventDto } from "@/shared/models/execution-event-dto";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

/**
 * Follows a run as it happens.
 *
 * Every row the page shows while running arrives here, including the hits a Find all search hands
 * out without executing anything - the engine reports those the same way, so nothing here has to
 * know they are special.
 */
export function useExecutionEvents(flowId: number) {
  const queryClient = useQueryClient();

  const {
    addExecutionStep,
    setRunState,
    setCurrentStep,
  } = useExecutionStore();

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.EXECUTION_EVENT) return;

      const event = message.payload as ExecutionEventDto;

      if (event.type === ExecutionEventTypeEnum.STEP_STARTED) {
        setCurrentStep(event.flowStepId ?? undefined, event.name);
        return;
      }

      if (event.type === ExecutionEventTypeEnum.STEP_FINISHED) {
        addExecutionStep(toExecutionStep(event));
        return;
      }

      // Parked before a step, so the step has not run and there is no row for it yet. The tree
      // marks where the run is sitting instead.
      if (event.type === ExecutionEventTypeEnum.PAUSED) {
        setRunState(RunStateEnum.PAUSED);
        setCurrentStep(event.flowStepId ?? undefined, event.name);
        return;
      }

      if (event.type === ExecutionEventTypeEnum.RUN_ENDED) {
        setRunState(RunStateEnum.FINISHED);
        setCurrentStep(undefined, undefined);

        // The run is only in the history list once it has ended.
        queryClient.invalidateQueries({ queryKey: executionKeys.list(flowId) });
      }
    });

    return () => {
      unsubscribe?.();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [flowId]);
}

/**
 * The event carries everything the row does, because both are built from the same execution step.
 * Only the database id is missing, and a live row has not got one yet.
 */
function toExecutionStep(event: ExecutionEventDto): ExecutionStepDto {
  return {
    id: 0,
    sequence: event.sequence ?? 0,
    parentSequence: event.parentSequence,
    depth: event.depth ?? 0,
    loopPass: event.loopPass,

    name: event.name,
    flowStepType: event.flowStepType,
    outcome: event.outcome ?? StepOutcomeEnum.SUCCESS,
    startedOn: new Date().toISOString(),
    durationMilliseconds: event.durationMilliseconds ?? 0,

    resultLocationX: event.resultLocationX,
    resultLocationY: event.resultLocationY,
    matchIndex: event.matchIndex,
    matchCount: event.matchCount,

    value: event.value,
    message: event.message,

    executionId: event.executionId,
    flowStepId: event.flowStepId,
  };
}
