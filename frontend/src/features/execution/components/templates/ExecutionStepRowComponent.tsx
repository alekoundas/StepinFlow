import { memo, useState } from "react";

import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { tint } from "@/shared/models/flow-step-catalog";
import {
  chipStyle,
  DONE_COLOR,
  FATAL_COLOR,
  FOUND_COLOR,
  HANDLED_COLOR,
  MONO,
  MUTED,
} from "@/features/execution/execution-format";
import { isCheckStep, isServedHit } from "@/features/execution/hooks/use-execution-overview";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

const NEUTRAL_COLOR = "var(--text-color-secondary)";

interface Props {
  executionStep: ExecutionStepDto;

  /** Every row the same, so the list can draw only those in view. */
  height: number;

  /** The one failure that ended the execution. Every other failure was caught by a Failure branch. */
  isFatal: boolean;
  isSelected: boolean;

  /** The slowest step of the execution, so every bar is drawn against the same scale. */
  maxDurationMilliseconds: number;

  onSelect: (sequence: number) => void;
}

/**
 * One row of the execution.
 *
 * Two failures can be on screen at once and they must not look the same: a step that failed into
 * its Failure branch is the flow working, and only the one that ended the execution is red.
 * Colouring both the same makes a healthy bot loop look like a disaster.
 */
function ExecutionStepRowComponent({
  executionStep,
  height,
  isFatal,
  isSelected,
  maxDurationMilliseconds,
  onSelect,
}: Props) {
  const [isHovered, setIsHovered] = useState(false);

  const isFailure = executionStep.outcome === StepOutcomeEnum.FAILURE;
  const isLoop = executionStep.flowStepType === FlowStepTypeEnum.LOOP;
  const isFound = isCheckStep(executionStep) && !isFailure;
  const isServed = isServedHit(executionStep);

  let icon = "pi-check";
  let iconColor = DONE_COLOR;
  if (isLoop) {
    icon = "pi-sync";
    iconColor = NEUTRAL_COLOR;
  } else if (isFailure) {
    icon = "pi-times";
    iconColor = isFatal ? FATAL_COLOR : HANDLED_COLOR;
  }

  let background: string | undefined;
  if (isSelected) background = tint(FOUND_COLOR, 12);
  else if (isHovered) background = "var(--surface-hover)";

  return (
    <div
      role="button"
      tabIndex={0}
      aria-pressed={isSelected}
      onClick={() => onSelect(executionStep.sequence)}
      onKeyDown={(event) => {
        if (event.key !== "Enter" && event.key !== " ") return;

        event.preventDefault();
        onSelect(executionStep.sequence);
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className="flex align-items-center gap-2 border-round-lg cursor-pointer"
      style={{
        height,
        boxSizing: "border-box",
        padding: "0 10px",
        whiteSpace: "nowrap",
        border: `1px solid ${isSelected ? tint(FOUND_COLOR, 55) : "transparent"}`,
        background,
        fontSize: "0.8rem",
        opacity: isServed ? 0.62 : 1,
      }}
    >
      <span
        style={{ ...MUTED, width: 48, flex: "none", textAlign: "right", fontFamily: MONO, fontSize: 11 }}
      >
        {executionStep.sequence.toLocaleString()}
      </span>

      <i
        className={`pi ${icon}`}
        style={{ color: iconColor, fontSize: "0.75rem", width: 14, flex: "none" }}
      />

      {/* One line per level, so a row's place in the tree reads without counting the indent. */}
      <span
        aria-hidden="true"
        className="flex align-self-stretch"
        style={{ flex: "none" }}
      >
        {Array.from({ length: executionStep.depth }, (_, i) => (
          <span
            key={i}
            style={{ width: 13, borderLeft: "1px solid var(--surface-border)" }}
          />
        ))}
      </span>

      <span
        style={{
          flex: "0 1 auto",
          minWidth: 64,
          overflow: "hidden",
          textOverflow: "ellipsis",
          color: isFailure || isLoop ? NEUTRAL_COLOR : undefined,
          fontWeight: isFound ? 600 : undefined,
        }}
      >
        {executionStep.name}
      </span>

      {isFound && executionStep.bestScore !== null && executionStep.bestScore !== undefined ? (
        <span style={{ ...chipStyle(FOUND_COLOR), flex: "none" }}>
          found {executionStep.bestScore.toFixed(2)}
        </span>
      ) : null}

      {executionStep.matchCount ? (
        <span style={{ ...chipStyle(FOUND_COLOR), flex: "none" }}>
          hit {(executionStep.matchIndex ?? 0) + 1} of {executionStep.matchCount}
        </span>
      ) : null}

      {isFailure && !isFatal ? (
        <span style={{ ...chipStyle(HANDLED_COLOR), flex: "none" }}>handled</span>
      ) : null}

      {isFatal ? (
        <span style={{ ...chipStyle(FATAL_COLOR), flex: "none" }}>ended the execution</span>
      ) : null}

      {executionStep.loopPass !== null && executionStep.loopPass !== undefined ? (
        <span style={{ ...chipStyle(NEUTRAL_COLOR), flex: "none" }}>
          pass {(executionStep.loopPass + 1).toLocaleString()}
        </span>
      ) : null}

      {/* What a Break left, so the execution reads without opening the flow. */}
      {executionStep.flowStepType === FlowStepTypeEnum.BREAK && executionStep.message ? (
        <span
          style={{ ...chipStyle(NEUTRAL_COLOR), flex: "0 1 auto", minWidth: 0, maxWidth: 220 }}
          title={executionStep.message}
        >
          {executionStep.message}
        </span>
      ) : null}

      {/* Where the time went, without reading a single number. */}
      <span
        style={{
          flex: "none",
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
            width:
              maxDurationMilliseconds > 0
                ? `${(executionStep.durationMilliseconds / maxDurationMilliseconds) * 100}%`
                : "0%",
            background: isFatal ? FATAL_COLOR : "var(--surface-400)",
          }}
        />
      </span>

      <span
        style={{
          ...MUTED,
          flex: "none",
          width: 58,
          textAlign: "right",
          fontFamily: MONO,
          fontSize: 11,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {isServed ? "—" : `${executionStep.durationMilliseconds} ms`}
      </span>
    </div>
  );
}

// A row never changes once it has arrived, so a new batch draws only the new rows - unless one of
// them is the slowest yet, which rescales every bar.
export default memo(ExecutionStepRowComponent);
