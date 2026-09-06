import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionStepRowComponent from "@/features/execution/components/templates/ExecutionStepRowComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";

type EmptyReason = "idle" | "loading" | "error" | "noHistory";

const emptyTitle = (reason: EmptyReason): string => {
  switch (reason) {
    case "loading":
      return "Loading the run...";
    case "error":
      return "That run could not be loaded.";
    case "noHistory":
      return "This run kept no steps.";
    default:
      return "Nothing has run yet.";
  }
};

const emptyDetail = (reason: EmptyReason): string => {
  switch (reason) {
    case "loading":
      return "Reading the steps it saved.";
    case "error":
      return "It may have been deleted, or the backend is not reachable.";
    case "noHistory":
      return "History was off when it ran, so only the outcome was saved.";
    default:
      return "Start the flow, or open a past run from History.";
  }
};

interface Props {
  /** The step that ended the run, when there was one. Comes off the Execution, not off a step. */
  errorFlowStepId?: number | null;

  /** Hide everything that succeeded, for a long run where only the failures matter. */
  showFailuresOnly?: boolean;

  /**
   * Why there is nothing to show, when there is nothing to show. A run that failed to load and a
   * run that kept no history look identical otherwise, and the difference is the whole answer.
   */
  emptyReason?: EmptyReason;

  /** What the backend said, when it said anything. Better than a guess about what went wrong. */
  emptyMessage?: string;
}

/**
 * The run, one row per thing that happened, in the order it happened.
 *
 * Ordered by sequence and indented by depth, which is why both are stored: no joins and no walking
 * a parent chain to draw a tree.
 */
export default function ExecutionStepListComponent({
  errorFlowStepId,
  showFailuresOnly = false,
  emptyReason = "idle",
  emptyMessage,
}: Props) {
  const {
    executionSteps,
    selectedSequence,
    runState,
    currentStepName,
    setSelectedSequence,
  } = useExecutionStore();

  if (executionSteps.length === 0)
    return (
      <div className="flex flex-column align-items-center justify-content-center gap-2 p-6 text-center">
        <IconComponent
          name={emptyReason === "error" ? "exclamation-triangle" : "play"}
          size="lg"
          className="text-color-secondary opacity-50"
        />
        <LabelComponent text={emptyTitle(emptyReason)} color="secondary" />
        <LabelComponent
          text={emptyMessage ?? emptyDetail(emptyReason)}
          size="sm"
          color="secondary"
        />
      </div>
    );

  const rows = showFailuresOnly
    ? executionSteps.filter((x) => x.outcome === StepOutcomeEnum.FAILURE)
    : executionSteps;

  // Scaled against the whole run, not the filtered view, so hiding rows does not restretch the bars.
  const maxDurationMilliseconds = Math.max(
    ...executionSteps.map((x) => x.durationMilliseconds),
    1,
  );

  return (
    <div className="flex flex-column p-2">
      {rows.map((executionStep) => (
        <ExecutionStepRowComponent
          key={executionStep.sequence}
          executionStep={executionStep}
          isFatal={!!errorFlowStepId && executionStep.flowStepId === errorFlowStepId}
          isSelected={executionStep.sequence === selectedSequence}
          maxDurationMilliseconds={maxDurationMilliseconds}
          onSelect={setSelectedSequence}
        />
      ))}

      {/* Parked before a step, so it has no row yet - there is nothing to report until it runs. */}
      {runState === RunStateEnum.PAUSED && currentStepName ? (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.5rem",
            padding: "0.25rem 0.5rem",
            fontFamily: "var(--font-family-monospace, monospace)",
            fontSize: "0.78rem",
          }}
        >
          <span style={{ minWidth: "1.6rem" }} />
          <i
            className="pi pi-circle-fill text-yellow-500"
            style={{ fontSize: "0.6rem" }}
          />
          <span className="text-yellow-500">{currentStepName}</span>
          <span
            style={{
              marginLeft: "auto",
              paddingLeft: "0.75rem",
              color: "var(--text-color-secondary)",
            }}
          >
            —
          </span>
        </div>
      ) : null}
    </div>
  );
}
