import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { backendApiService } from "@/shared/services/backend-api-service";
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
    setExecutionSteps,
    setRunState,
    setCurrentStep,
  } = useExecutionStore();

  /**
   * A broadcast carries no database id and nothing the history filled in - the screenshot a step
   * kept, its exit code, its stderr. With history on the saved rows are the same steps with those
   * present, and the engine has flushed them all before it says the run ended.
   */
  const replaceWithSavedSteps = async (executionId: number) => {
    const execution = await queryClient.fetchQuery({
      queryKey: executionKeys.detail(executionId),
      queryFn: () => backendApiService.Execution.get(executionId),
    });

    // History off writes nothing, and the live rows are then all there is.
    if (execution.executionSteps.length > 0)
      setExecutionSteps(execution.executionSteps);
  };

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

        void replaceWithSavedSteps(event.executionId);
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
