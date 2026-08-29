import { Button } from "primereact/button";
import { Dropdown } from "primereact/dropdown";

import StatusPillComponent, {
  type StatusPillSeverity,
} from "@/shared/components/StatusPillComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionMutations } from "@/features/execution/hooks/use-execution";
import type { ExecutionStartDto } from "@/shared/models/execution-start-dto";

interface Props {
  flowId: number;
}

const HISTORY_LEVEL_OPTIONS = [
  { label: "Steps and images", value: ExecutionHistoryLevelEnum.STEPS_AND_IMAGES },
  { label: "Steps only", value: ExecutionHistoryLevelEnum.STEPS },
  { label: "Nothing", value: ExecutionHistoryLevelEnum.NONE },
];

/**
 * Start, and the debugger. Which buttons are live is decided by the run state alone, so the page
 * never has to keep a second idea of what is going on.
 */
export default function ExecutionToolbarComponent({ flowId }: Props) {
  const {
    runState,
    breakpointStepIds,
    historyLevel,
    executionSteps,
    currentStepName,
    setHistoryLevel,
    setRunState,
    setExecutionId,
    resetRun,
  } = useExecutionStore();

  const {
    startExecutionMutation,
    stopExecutionMutation,
    pauseExecutionMutation,
    continueExecutionMutation,
    stepIntoExecutionMutation,
    stepOverExecutionMutation,
  } = useExecutionMutations();

  const isRunning = runState !== RunStateEnum.FINISHED;
  const isPaused = runState === RunStateEnum.PAUSED;

  const handleStart = async () => {
    resetRun();

    const dto: ExecutionStartDto = {
      flowId: flowId,
      historyLevel: historyLevel,
      breakpoints: breakpointStepIds,
    };

    setRunState(RunStateEnum.RUNNING);

    const executionId = await startExecutionMutation.mutateAsync(dto);
    setExecutionId(executionId);
  };

  return (
    <div className="flex flex-wrap align-items-center gap-2 p-3 border-bottom-1 surface-border">
      {isPaused ? (
        <Button
          label="Continue"
          icon="pi pi-play"
          size="small"
          onClick={() => {
            setRunState(RunStateEnum.RUNNING);
            continueExecutionMutation.mutate();
          }}
        />
      ) : (
        <Button
          label="Start"
          icon="pi pi-play"
          size="small"
          disabled={isRunning}
          onClick={handleStart}
        />
      )}

      <Button
        label="Pause"
        icon="pi pi-pause"
        size="small"
        outlined
        disabled={!isRunning || isPaused}
        onClick={() => pauseExecutionMutation.mutate()}
      />

      <Button
        label="Step into"
        icon="pi pi-angle-double-down"
        size="small"
        outlined
        disabled={!isPaused}
        onClick={() => stepIntoExecutionMutation.mutate()}
      />

      <Button
        label="Step over"
        icon="pi pi-angle-double-right"
        size="small"
        outlined
        disabled={!isPaused}
        onClick={() => stepOverExecutionMutation.mutate()}
      />

      <Button
        label="Stop"
        icon="pi pi-stop"
        size="small"
        outlined
        severity="danger"
        disabled={!isRunning}
        onClick={() => stopExecutionMutation.mutate()}
      />

      <div className="flex-auto" />

      <Dropdown
        value={historyLevel}
        options={HISTORY_LEVEL_OPTIONS}
        disabled={isRunning}
        onChange={(e) => setHistoryLevel(e.value)}
        className="p-inputtext-sm"
      />

      <StatusPillComponent
        text={statusText(runState, executionSteps.length, currentStepName)}
        severity={statusSeverity(runState)}
        pulse={runState === RunStateEnum.RUNNING}
      />
    </div>
  );
}

function statusSeverity(runState: RunStateEnum): StatusPillSeverity {
  switch (runState) {
    case RunStateEnum.RUNNING:
      return "running";
    case RunStateEnum.PAUSED:
      return "paused";
    case RunStateEnum.STOPPING:
      return "danger";
    default:
      return "neutral";
  }
}

function statusText(
  runState: RunStateEnum,
  stepCount: number,
  currentStepName: string | undefined,
): string {
  switch (runState) {
    case RunStateEnum.RUNNING:
      return `Running · ${stepCount} steps`;
    case RunStateEnum.PAUSED:
      return currentStepName ? `Paused on ${currentStepName}` : "Paused";
    case RunStateEnum.STOPPING:
      return "Stopping";
    default:
      return stepCount > 0 ? `Finished · ${stepCount} steps` : "Idle";
  }
}
