import { memo, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionStepRowComponent from "@/features/execution/components/templates/ExecutionStepRowComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

type EmptyReason = "idle" | "loading" | "error" | "noHistory";

// Every row is one line this tall, so only the rows in view are drawn. An execution left on for
// hours has tens of thousands, and drawing them all is what made the page lag.
const ROW_HEIGHT = 30;

// Drawn beyond each edge, so a fast scroll shows no blank band.
const OVERSCAN_ROWS = 10;

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
 * The run, one row per thing that happened, newest first: the step that just executed is the top
 * row.
 *
 * Ordered by sequence and indented by depth, which is why both are stored: no joins and no walking
 * a parent chain to draw a tree.
 */
function ExecutionStepListComponent({
  errorFlowStepId,
  showFailuresOnly = false,
  emptyReason = "idle",
  emptyMessage,
}: Props) {
  const executionSteps = useExecutionStore((x) => x.executionSteps);
  const selectedSequence = useExecutionStore((x) => x.selectedSequence);
  const runState = useExecutionStore((x) => x.runState);
  const currentStepName = useExecutionStore((x) => x.currentStepName);
  const setSelectedSequence = useExecutionStore((x) => x.setSelectedSequence);

  const viewportRef = useRef<HTMLDivElement>(null);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(0);
  const isEmpty = executionSteps.length === 0;

  const rows = useMemo(() => {
    const shown = showFailuresOnly
      ? executionSteps.filter((x) => x.outcome === StepOutcomeEnum.FAILURE)
      : executionSteps;

    return [...shown].reverse();
  }, [executionSteps, showFailuresOnly]);

  // Scaled against the whole run, not the filtered view, so hiding rows does not restretch the bars.
  // Not Math.max(...rows): a long run has more rows than a call takes arguments.
  const maxDurationMilliseconds = useMemo(
    () => executionSteps.reduce((max, x) => Math.max(max, x.durationMilliseconds), 1),
    [executionSteps],
  );

  useLayoutEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    // Also on first sight, so a new run's list does not start at the last one's scroll.
    const observer = new ResizeObserver(() => {
      setViewportHeight(viewport.clientHeight);
      setScrollTop(viewport.scrollTop);
    });
    observer.observe(viewport);

    return () => observer.disconnect();
  }, [isEmpty]);

  // Scrolled down to read, the rows arriving on top would carry what is being read away.
  const topRow = useRef<ExecutionStepDto | undefined>(undefined);
  useLayoutEffect(() => {
    const viewport = viewportRef.current;
    const previousTop = topRow.current;
    topRow.current = rows[0];

    if (!viewport || viewport.scrollTop === 0 || !previousTop) return;

    const added = rows.indexOf(previousTop);
    if (added > 0) viewport.scrollTop += added * ROW_HEIGHT;
  }, [rows]);

  // Jump to failure picks a row that can be far out of view.
  useEffect(() => {
    const viewport = viewportRef.current;
    const index = rows.findIndex((x) => x.sequence === selectedSequence);
    if (!viewport || index < 0) return;

    const top = index * ROW_HEIGHT;
    const isInView =
      top >= viewport.scrollTop && top + ROW_HEIGHT <= viewport.scrollTop + viewport.clientHeight;
    if (!isInView) viewport.scrollTop = top;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedSequence]);

  if (isEmpty)
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

  const first = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN_ROWS);
  const last = Math.min(
    rows.length,
    Math.ceil((scrollTop + viewportHeight) / ROW_HEIGHT) + OVERSCAN_ROWS,
  );

  return (
    <div className="flex flex-column h-full">
      {/* Parked before a step, so it has no row yet - there is nothing to report until it runs. */}
      {runState === RunStateEnum.PAUSED && currentStepName ? (
        <div
          className="mx-2 mt-2"
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

      <div
        ref={viewportRef}
        className="flex-1 p-2"
        style={{ overflow: "auto", minHeight: 0, overflowAnchor: "none" }}
        onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}
      >
        <div style={{ position: "relative", height: rows.length * ROW_HEIGHT }}>
          <div style={{ position: "absolute", top: first * ROW_HEIGHT, left: 0, right: 0 }}>
            {rows.slice(first, last).map((executionStep) => (
              <ExecutionStepRowComponent
                key={executionStep.sequence}
                executionStep={executionStep}
                height={ROW_HEIGHT}
                isFatal={!!errorFlowStepId && executionStep.flowStepId === errorFlowStepId}
                isSelected={executionStep.sequence === selectedSequence}
                maxDurationMilliseconds={maxDurationMilliseconds}
                onSelect={setSelectedSequence}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export default memo(ExecutionStepListComponent);
