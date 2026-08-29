import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionStepRowComponent from "@/features/execution/components/templates/ExecutionStepRowComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";

interface Props {
  /** The step that ended the run, when there was one. Comes off the Execution, not off a step. */
  errorFlowStepId?: number | null;
}

/**
 * The run, one row per thing that happened, in the order it happened.
 *
 * Ordered by sequence and indented by depth, which is why both are stored: no joins and no walking
 * a parent chain to draw a tree.
 */
export default function ExecutionStepListComponent({ errorFlowStepId }: Props) {
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

  return (
    <div className="flex flex-column p-2">
      {executionSteps.map((executionStep) => (
        <ExecutionStepRowComponent
          key={executionStep.sequence}
          executionStep={executionStep}
          isFatal={
            !!errorFlowStepId && executionStep.flowStepId === errorFlowStepId
          }
          isSelected={executionStep.sequence === selectedSequence}
          onSelect={setSelectedSequence}
        />
      ))}

      {/* Parked before a step, so it has no row yet - there is nothing to report until it runs. */}
      {runState === RunStateEnum.PAUSED && currentStepName ? (
        <div className="execution-step-row">
          <span className="execution-step-sequence" />
          <i
            className="pi pi-circle-fill text-yellow-500"
            style={{ fontSize: "0.6rem" }}
          />
          <span className="text-yellow-500">{currentStepName}</span>
          <span className="execution-step-duration">—</span>
        </div>
      ) : null}
    </div>
  );
}
