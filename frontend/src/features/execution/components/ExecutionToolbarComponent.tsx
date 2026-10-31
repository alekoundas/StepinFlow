import { useEffect } from "react";
import { Button } from "primereact/button";
import { Dropdown } from "primereact/dropdown";

import DisabledTooltipComponent from "@/shared/components/DisabledTooltipComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import { matchesHotkey, toHotkeyDisplay } from "@/shared/utils/hotkey";
import { MONO, MUTED } from "@/features/execution/execution-format";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionMutations } from "@/features/execution/hooks/use-execution";
import { useDebuggerKeys } from "@/features/execution/hooks/use-debugger-keys";
import { useFlowValidation } from "@/features/flow/hooks/use-flow-validation";
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
 * Start, and the debugger. Which buttons are live is decided by the execution state alone, so the
 * page never has to keep a second idea of what is going on.
 *
 * The keys work while StepinFlow has focus. Matching them anywhere waits on the hook (TODO.md,
 * Hotkeys).
 */
export default function ExecutionToolbarComponent({ flowId }: Props) {
  const runState = useExecutionStore((x) => x.runState);
  const breakpointStepIds = useExecutionStore((x) => x.breakpointStepIds);
  const historyLevel = useExecutionStore((x) => x.historyLevel);
  const setHistoryLevel = useExecutionStore((x) => x.setHistoryLevel);
  const setRunState = useExecutionStore((x) => x.setRunState);
  const setExecutionId = useExecutionStore((x) => x.setExecutionId);
  const resetRun = useExecutionStore((x) => x.resetRun);

  const {
    startExecutionMutation,
    stopExecutionMutation,
    pauseExecutionMutation,
    continueExecutionMutation,
    stepIntoExecutionMutation,
    stepOverExecutionMutation,
  } = useExecutionMutations();

  const { openConfirm } = useDialogStore();
  const keys = useDebuggerKeys();

  // The same answer Start is refused on, so the button and the backend cannot disagree.
  const { data: validation } = useFlowValidation(flowId);
  const hasErrors = validation?.hasErrors ?? false;

  const isIdle = runState === RunStateEnum.FINISHED;
  const isPaused = runState === RunStateEnum.PAUSED;
  const canPause = runState === RunStateEnum.RUNNING;
  const canStop = !isIdle && runState !== RunStateEnum.STOPPING;

  const handleStart = async () => {
    resetRun();

    const dto: ExecutionStartDto = {
      flowId: flowId,
      historyLevel: historyLevel,
      breakpoints: breakpointStepIds,
    };

    setRunState(RunStateEnum.RUNNING);

    try {
      const executionId = await startExecutionMutation.mutateAsync(dto);
      setExecutionId(executionId);
    } catch (err) {
      setRunState(RunStateEnum.FINISHED);
      openConfirm("execution-start-error", {
        headerText: "The flow did not start",
        hideConfirm: true,
        cancelLabel: "Close",
        children: <LabelComponent text={err instanceof Error ? err.message : String(err)} />,
      });
    }
  };

  const handleContinue = () => {
    setRunState(RunStateEnum.RUNNING);
    continueExecutionMutation.mutate();
  };

  const handleStop = () => {
    // Said at once rather than when the engine's last events have arrived.
    setRunState(RunStateEnum.STOPPING);
    stopExecutionMutation.mutate();
  };

  // Bound again on every draw, so each press acts on the state as it is now.
  useEffect(() => {
    const actions = [
      { keys: keys.continue, isEnabled: isPaused, act: handleContinue },
      { keys: keys.stepInto, isEnabled: isPaused, act: () => stepIntoExecutionMutation.mutate() },
      { keys: keys.stepOver, isEnabled: isPaused, act: () => stepOverExecutionMutation.mutate() },
      { keys: keys.pause, isEnabled: canPause, act: () => pauseExecutionMutation.mutate() },
      { keys: keys.stop, isEnabled: canStop, act: handleStop },
    ];

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.repeat) return;

      // A letter set as a key is still a letter in Find a step.
      const target = event.target as HTMLElement | null;
      const isTyping =
        !!target && (target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName));
      if (isTyping && !/^F\d+$/.test(event.code)) return;

      const action = actions.find((x) => x.isEnabled && matchesHotkey(event, x.keys));
      if (!action) return;

      event.preventDefault();
      action.act();
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  });

  if (isIdle)
    return (
      <div className="flex flex-wrap align-items-center gap-2">
        <span
          className="text-sm"
          style={MUTED}
        >
          Keep
        </span>
        <Dropdown
          value={historyLevel}
          options={HISTORY_LEVEL_OPTIONS}
          onChange={(e) => setHistoryLevel(e.value)}
          className="p-inputtext-sm"
        />

        <DisabledTooltipComponent
          tooltip={hasErrors ? "Fix the errors in this flow before executing it" : null}
        >
          <Button
            label="Start"
            icon="pi pi-play"
            size="small"
            disabled={hasErrors}
            onClick={handleStart}
          />
        </DisabledTooltipComponent>
      </div>
    );

  return (
    <div
      role="toolbar"
      aria-label="Debugger"
      className="flex flex-wrap align-items-center gap-2"
    >
      {isPaused ? (
        <Button
          label="Continue"
          icon="pi pi-play"
          size="small"
          onClick={handleContinue}
        >
          <KeyChip keys={keys.continue} />
        </Button>
      ) : (
        <Button
          label="Pause"
          icon="pi pi-pause"
          size="small"
          outlined
          disabled={!canPause}
          onClick={() => pauseExecutionMutation.mutate()}
        >
          <KeyChip keys={keys.pause} />
        </Button>
      )}

      <Button
        label="Step into"
        icon="pi pi-arrow-down"
        size="small"
        outlined
        disabled={!isPaused}
        onClick={() => stepIntoExecutionMutation.mutate()}
      >
        <KeyChip keys={keys.stepInto} />
      </Button>

      <Button
        label="Step over"
        icon="pi pi-replay"
        size="small"
        outlined
        disabled={!isPaused}
        onClick={() => stepOverExecutionMutation.mutate()}
      >
        <KeyChip keys={keys.stepOver} />
      </Button>

      <span
        aria-hidden="true"
        className="mx-1"
        style={{ width: 1, height: 24, background: "var(--surface-border)" }}
      />

      <Button
        label="Stop"
        icon="pi pi-stop"
        size="small"
        outlined
        severity="danger"
        disabled={!canStop}
        onClick={handleStop}
      >
        <KeyChip keys={keys.stop} />
      </Button>
    </div>
  );
}

interface KeyChipProps {
  keys?: string;
}

function KeyChip({ keys }: KeyChipProps) {
  if (!keys) return null;

  return (
    <kbd
      className="ml-2 px-1 border-1 border-round-sm"
      style={{ fontFamily: MONO, fontSize: 11, opacity: 0.7, borderColor: "currentColor" }}
    >
      {toHotkeyDisplay(keys)}
    </kbd>
  );
}
