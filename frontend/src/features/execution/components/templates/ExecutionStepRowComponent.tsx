import { useState } from "react";
import { classNames } from "primereact/utils";

import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { hasBranches } from "@/shared/models/flow-step-catalog";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  executionStep: ExecutionStepDto;

  /** The one failure that ended the run. Every other failure was caught by a Failure branch. */
  isFatal: boolean;
  isSelected: boolean;

  /** The slowest step of the run, so every bar is drawn against the same scale. */
  maxDurationMilliseconds: number;

  onSelect: (sequence: number) => void;
}

/**
 * One row of the run.
 *
 * Two failures can be on screen at once and they must not look the same: a step that failed into
 * its Failure branch is the flow working, and only the one that ended the run is red. Colouring
 * both the same makes a healthy retry loop look like a disaster.
 *
 * Monospace because it is columnar - sequences, types and durations all line up.
 */
export default function ExecutionStepRowComponent({
  executionStep,
  isFatal,
  isSelected,
  maxDurationMilliseconds,
  onSelect,
}: Props) {
  const [isHovered, setIsHovered] = useState(false);

  const isFailure = executionStep.outcome === StepOutcomeEnum.FAILURE;

  // A hit handed out from an earlier search's screenshot. Nothing ran, so it carries no duration.
  const isServed =
    executionStep.matchIndex !== null &&
    executionStep.matchIndex !== undefined &&
    executionStep.matchIndex > 0;

  const mutedStyle = {
    color: "var(--text-color-secondary)",
    fontVariantNumeric: "tabular-nums" as const,
  };

  return (
    <div
      onClick={() => onSelect(executionStep.sequence)}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "0.5rem",
        padding: "0.25rem 0.5rem",
        borderRadius: 4,
        border: "1px solid transparent",
        borderColor: isSelected ? "var(--primary-color)" : "transparent",
        background: isHovered ? "var(--surface-hover)" : undefined,
        fontFamily: "var(--font-family-monospace, monospace)",
        fontSize: "0.78rem",
        cursor: "pointer",
        opacity: isServed ? 0.62 : 1,
      }}
    >
      <span
        style={{
          ...mutedStyle,
          opacity: 0.6,
          textAlign: "right",
          minWidth: "1.6rem",
        }}
      >
        {executionStep.sequence}
      </span>

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

      <span
        style={{
          color: "var(--text-color-secondary)",
          opacity: 0.55,
          fontSize: "0.68rem",
        }}
      >
        {executionStep.flowStepType}
      </span>

      {/* Where the time went, without reading a single number. */}
      <span
        style={{
          width: 56,
          height: 4,
          marginLeft: "auto",
          borderRadius: 2,
          background: "var(--surface-ground)",
          overflow: "hidden",
        }}
      >
        <span
          style={{
            display: "block",
            height: "100%",
            borderRadius: 2,
            width:
              maxDurationMilliseconds > 0
                ? `${(executionStep.durationMilliseconds / maxDurationMilliseconds) * 100}%`
                : "0%",
            background: isFatal ? "var(--red-400)" : "var(--surface-border)",
          }}
        />
      </span>

      <span style={{ ...mutedStyle, paddingLeft: "0.75rem" }}>
        {executionStep.durationMilliseconds}ms
      </span>
    </div>
  );
}
