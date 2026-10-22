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

// At most this often, however fast the engine sends them. Drawing on every event cost more than a
// fast flow left between two, and the page fell further and further behind.
const APPLY_INTERVAL_MILLISECONDS = 100;

/**
 * Follows a run as it happens.
 *
 * Every row the page shows while running arrives here, including the hits a Find all search hands
 * out without executing anything - the engine reports those the same way, so nothing here has to
 * know they are special.
 *
 * Events are collected and applied together on a screen refresh, so a window nobody can see draws
 * nothing and catches up in one go when it is shown.
 */
export function useExecutionEvents(flowId: number) {
  const queryClient = useQueryClient();

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
      useExecutionStore.getState().setExecutionSteps(execution.executionSteps);
  };

  const apply = (events: ExecutionEventDto[]) => {
    const finished: ExecutionStepDto[] = [];
    let current: { flowStepId: number | undefined; name: string | undefined } | null = null;
    let runState: RunStateEnum | null = null;
    let endedExecutionId: number | null = null;

    for (const event of events) {
      if (event.type === ExecutionEventTypeEnum.STEP_STARTED) {
        current = { flowStepId: event.flowStepId ?? undefined, name: event.name };

        // Paused and going again within one batch, so it is not paused any more.
        if (runState === RunStateEnum.PAUSED) runState = null;
      }

      if (event.type === ExecutionEventTypeEnum.STEP_FINISHED) finished.push(toExecutionStep(event));

      // Parked before a step, so the step has not run and there is no row for it yet. The tree
      // marks where the run is sitting instead.
      if (event.type === ExecutionEventTypeEnum.PAUSED) {
        runState = RunStateEnum.PAUSED;
        current = { flowStepId: event.flowStepId ?? undefined, name: event.name };
      }

      if (event.type === ExecutionEventTypeEnum.RUN_ENDED) {
        runState = RunStateEnum.FINISHED;
        current = { flowStepId: undefined, name: undefined };
        endedExecutionId = event.executionId;
      }
    }

    const store = useExecutionStore.getState();

    if (finished.length > 0) store.addExecutionSteps(finished);
    if (current) store.setCurrentStep(current.flowStepId, current.name);
    if (runState) store.setRunState(runState);

    if (endedExecutionId !== null) {
      // The run is only in the history list once it has ended.
      queryClient.invalidateQueries({ queryKey: executionKeys.list(flowId) });

      void replaceWithSavedSteps(endedExecutionId);
    }
  };

  useEffect(() => {
    let pending: ExecutionEventDto[] = [];
    let frame: number | null = null;
    let appliedAt = 0;

    const onFrame = (now: number) => {
      frame = null;

      if (now - appliedAt < APPLY_INTERVAL_MILLISECONDS) {
        frame = requestAnimationFrame(onFrame);
        return;
      }

      appliedAt = now;
      const events = pending;
      pending = [];
      apply(events);
    };

    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.EXECUTION_EVENT) return;

      pending.push(message.payload as ExecutionEventDto);
      if (frame === null) frame = requestAnimationFrame(onFrame);
    });

    return () => {
      unsubscribe?.();
      if (frame !== null) cancelAnimationFrame(frame);
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
