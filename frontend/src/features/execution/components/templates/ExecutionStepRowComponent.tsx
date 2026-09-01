import { classNames } from "primereact/utils";

import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { hasBranches } from "@/shared/models/flow-step-catalog";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  executionStep: ExecutionStepDto;

  /** The one failure that ended the run. Every other failure was caught by a Failure branch. */
  isFatal: boolean;
  isSelected: boolean;

  onSelect: (sequence: number) => void;
}

/**
 * One row of the run.
 *
 * Two failures can be on screen at once and they must not look the same: a step that failed into
 * its Failure branch is the flow working, and only the one that ended the run is red. Colouring
 * both the same makes a healthy retry loop look like a disaster.
 */
export default function ExecutionStepRowComponent({
  executionStep,
  isFatal,
  isSelected,
  onSelect,
}: Props) {
  const isFailure = executionStep.outcome === StepOutcomeEnum.FAILURE;

  // A hit handed out from an earlier search's screenshot. Nothing ran, so it carries no duration.
  const isServed =
    executionStep.matchIndex !== null &&
    executionStep.matchIndex !== undefined &&
    executionStep.matchIndex > 0;

  return (
    <div
      className={classNames("execution-step-row", {
        "execution-step-row-selected": isSelected,
        "execution-step-row-served": isServed,
      })}
      onClick={() => onSelect(executionStep.sequence)}
    >
      <span className="execution-step-sequence">{executionStep.sequence}</span>

      <i
        className={classNames("pi", {
          "pi-check text-green-500": !isFailure,
          "pi-times text-yellow-500": isFailure && !isFatal,
          "pi-times text-red-500": isFailure && isFatal,
        })}
        style={{ fontSize: "0.7rem" }}
      />

      <span style={{ paddingLeft: `${executionStep.depth}rem` }}>
        {executionStep.name}
      </span>

      {/* Only a branching step routes on its outcome. Everywhere else it is noise. */}
      {hasBranches(executionStep.flowStepType) ? (
        <span
          className={classNames("p-tag text-xs", {
            "p-tag-success": !isFailure,
            "p-tag-warning": isFailure && !isFatal,
            "p-tag-danger": isFailure && isFatal,
          })}
        >
          {isFailure ? "failed" : "success"}
        </span>
      ) : null}

      {executionStep.matchCount ? (
        <span className="p-tag p-tag-info text-xs">
          {(executionStep.matchIndex ?? 0) + 1} of {executionStep.matchCount}
        </span>
      ) : null}

      {executionStep.loopPass !== null && executionStep.loopPass !== undefined ? (
        <span className="p-tag text-xs">pass {executionStep.loopPass + 1}</span>
      ) : null}

      {isFailure && !isFatal ? (
        <span className="p-tag p-tag-warning text-xs">handled</span>
      ) : null}

      {isFatal ? (
        <span className="p-tag p-tag-danger text-xs">ended the run</span>
      ) : null}

      <span className="execution-step-type">{executionStep.flowStepType}</span>
      <span className="execution-step-duration">
        {executionStep.durationMilliseconds}ms
      </span>
    </div>
  );
}
