import LabelComponent from "@/shared/components/LabelComponent";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  executionSteps: ExecutionStepDto[];

  /** The step that ended the run, when there was one. Comes off the Execution, not off a step. */
  errorFlowStepId?: number | null;
}

/**
 * The shape of a run in one line, before any scrolling.
 *
 * The count that matters is the split between failures: a run with eleven handled failures and no
 * fatal one is a retry loop doing its job, and it reads as a disaster in a list of red rows.
 */
export default function RunSummaryComponent({
  executionSteps,
  errorFlowStepId,
}: Props) {
  if (executionSteps.length === 0) return null;

  const failures = executionSteps.filter(
    (x) => x.outcome === StepOutcomeEnum.FAILURE,
  );

  const fatalCount = errorFlowStepId
    ? failures.filter((x) => x.flowStepId === errorFlowStepId).length
    : 0;

  const handledCount = failures.length - fatalCount;

  const elapsedMilliseconds = executionSteps.reduce(
    (total, x) => total + x.durationMilliseconds,
    0,
  );

  return (
    <div className="flex flex-wrap align-items-center gap-4 mt-2">
      <Stat
        value={executionSteps.length}
        label="steps"
      />
      <Stat
        value={`${(elapsedMilliseconds / 1000).toFixed(1)}s`}
        label="elapsed"
      />

      {handledCount > 0 ? (
        <div className="flex align-items-center gap-2">
          <Dot colour="var(--orange-400)" />
          <LabelComponent
            text={`${handledCount} handled ${handledCount === 1 ? "failure" : "failures"}`}
            size="sm"
            color="warning"
          />
        </div>
      ) : null}

      {fatalCount > 0 ? (
        <div className="flex align-items-center gap-2">
          <Dot colour="var(--red-400)" />
          <LabelComponent
            text="1 ended the run"
            size="sm"
            color="error"
          />
        </div>
      ) : null}
    </div>
  );
}

interface StatProps {
  value: string | number;
  label: string;
}

function Stat({ value, label }: StatProps) {
  return (
    <div className="flex align-items-baseline gap-2">
      <LabelComponent
        text={value}
        weight="semibold"
      />
      <LabelComponent
        text={label}
        size="sm"
        color="secondary"
      />
    </div>
  );
}

interface DotProps {
  colour: string;
}

function Dot({ colour }: DotProps) {
  return (
    <span
      style={{
        width: 7,
        height: 7,
        borderRadius: "50%",
        background: colour,
      }}
    />
  );
}
