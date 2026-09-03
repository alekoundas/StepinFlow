import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionStepRowComponent from "@/features/execution/components/templates/ExecutionStepRowComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";

interface Props {
  /** The step that ended the run, when there was one. Comes off the Execution, not off a step. */
  errorFlowStepId?: number | null;

  /** Hide everything that succeeded, for a long run where only the failures matter. */
  showFailuresOnly?: boolean;
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
          name="play"
          size="lg"
          className="text-color-secondary opacity-50"
        />
        <LabelComponent text="Nothing has run yet." color="secondary" />
        <LabelComponent
          text="Start the flow, or open a past run from History."
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
        <div key={executionStep.sequence}>
          <ExecutionStepRowComponent
            executionStep={executionStep}
            isFatal={
              !!errorFlowStepId && executionStep.flowStepId === errorFlowStepId
            }
            isSelected={executionStep.sequence === selectedSequence}
            maxDurationMilliseconds={maxDurationMilliseconds}
            onSelect={setSelectedSequence}
          />

          {/* The whole diagnosis of a failed search, on the row that failed. */}
          {executionStep.outcome === StepOutcomeEnum.FAILURE &&
          executionStep.bestScore !== null &&
          executionStep.bestScore !== undefined ? (
            <div
              className="flex align-items-center gap-2 border-1 surface-border border-round px-3 py-2 mb-1"
              style={{ marginLeft: "3rem", background: "var(--surface-ground)" }}
            >
              <IconComponent
                name="info-circle"
                size="sm"
                className="text-orange-400"
              />
              <LabelComponent
                text={`Best match scored ${executionStep.bestScore.toFixed(2)}`}
                size="sm"
              />
            </div>
          ) : null}
        </div>
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
