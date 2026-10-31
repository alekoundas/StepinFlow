import { memo, useEffect, useLayoutEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Button } from "primereact/button";
import { IconField } from "primereact/iconfield";
import { InputIcon } from "primereact/inputicon";
import { InputText } from "primereact/inputtext";
import { SelectButton } from "primereact/selectbutton";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionPanelComponent from "@/features/execution/components/templates/ExecutionPanelComponent";
import ExecutionStepRowComponent from "@/features/execution/components/templates/ExecutionStepRowComponent";
import ExecutionFoldRowComponent from "@/features/execution/components/templates/ExecutionFoldRowComponent";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { tint } from "@/shared/models/flow-step-catalog";
import { toHotkeyDisplay } from "@/shared/utils/hotkey";
import {
  chipStyle,
  FOUND_COLOR,
  HANDLED_COLOR,
  MUTED,
} from "@/features/execution/execution-format";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useDebuggerKeys } from "@/features/execution/hooks/use-debugger-keys";
import {
  filterSteps,
  foldQuietPasses,
  type ExecutionListFilter,
  type ExecutionListItem,
  type ExecutionOverview,
} from "@/features/execution/hooks/use-execution-overview";

type EmptyReason = "idle" | "loading" | "error" | "noHistory";

// Every row is one line this tall, so only the rows in view are drawn. An execution left on for
// hours has tens of thousands, and drawing them all is what made the page lag.
const ROW_HEIGHT = 30;

// Drawn beyond each edge, so a fast scroll, or rows arriving above the ones being read, shows no
// blank band.
const OVERSCAN_ROWS = 20;

const FILTER_OPTIONS: { label: string; value: ExecutionListFilter }[] = [
  { label: "All", value: "all" },
  { label: "Found", value: "found" },
  { label: "Failed", value: "failed" },
];

const emptyTitle = (reason: EmptyReason): string => {
  switch (reason) {
    case "loading":
      return "Loading the execution...";
    case "error":
      return "That execution could not be loaded.";
    case "noHistory":
      return "This execution kept no steps.";
    default:
      return "Nothing has executed yet.";
  }
};

const emptyDetail = (reason: EmptyReason): string => {
  switch (reason) {
    case "loading":
      return "Reading the steps it saved.";
    case "error":
      return "It may have been deleted, or the backend is not reachable.";
    case "noHistory":
      return "It was set to keep nothing, so only the outcome was saved.";
    default:
      return "Start the flow, or open a past execution from Executions.";
  }
};

interface Props {
  overview: ExecutionOverview;

  /** Still adding rows, so the top is where to be. */
  isLive: boolean;

  /**
   * Why there is nothing to show, when there is nothing to show. An execution that failed to load
   * and one that kept no history look identical otherwise, and the difference is the whole answer.
   */
  emptyReason?: EmptyReason;

  /** What the backend said, when it said anything. Better than a guess about what went wrong. */
  emptyMessage?: string;
}

/**
 * The execution, one row per thing that happened, newest first: the step that just executed is the
 * top row. A run of loop passes that found nothing is one row, so the passes that did are not
 * buried under hours of looking.
 *
 * Indented by depth, which is why it is stored: no joins and no walking a parent chain to draw a
 * tree.
 */
function ExecutionStepListComponent({
  overview,
  isLive,
  emptyReason = "idle",
  emptyMessage,
}: Props) {
  const executionSteps = useExecutionStore((x) => x.executionSteps);
  const selectedSequence = useExecutionStore((x) => x.selectedSequence);
  const runState = useExecutionStore((x) => x.runState);
  const currentStepName = useExecutionStore((x) => x.currentStepName);
  const setSelectedSequence = useExecutionStore((x) => x.setSelectedSequence);

  const [filter, setFilter] = useState<ExecutionListFilter>("all");
  const [query, setQuery] = useState("");
  const [openFolds, setOpenFolds] = useState<ReadonlySet<number>>(() => new Set());

  const viewportRef = useRef<HTMLDivElement>(null);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(0);

  // How many rows there were when the top was left, so the way back can say how many came since.
  const [stepCountAtLeaving, setStepCountAtLeaving] = useState(0);

  const isEmpty = executionSteps.length === 0;
  const isFiltered = filter !== "all" || query.trim() !== "";

  const items = useMemo(() => {
    if (isFiltered) return filterSteps(executionSteps, filter, query);
    return foldQuietPasses(executionSteps, overview.quietPassEnds, openFolds);
  }, [executionSteps, overview.quietPassEnds, openFolds, filter, query, isFiltered]);

  // Scaled against the whole execution, not the filtered view, so hiding rows does not restretch
  // the bars. Not Math.max(...rows): a long execution has more rows than a call takes arguments.
  const maxDurationMilliseconds = useMemo(
    () => executionSteps.reduce((max, x) => Math.max(max, x.durationMilliseconds), 1),
    [executionSteps],
  );

  useLayoutEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    const observer = new ResizeObserver(() => {
      setViewportHeight(viewport.clientHeight);
      setScrollTop(viewport.scrollTop);
    });
    observer.observe(viewport);

    return () => observer.disconnect();
  }, [isEmpty]);

  // Scrolled down to read, the rows arriving on top would carry what is being read away. Kept by
  // what the top row is rather than where, since a pass that found nothing can fold under it.
  const anchor = useRef<Anchor | undefined>(undefined);
  useLayoutEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport || viewport.scrollTop === 0 || !anchor.current) return;

    const index = indexOfAnchor(items, anchor.current);
    if (index < 0) return;

    const top = index * ROW_HEIGHT + anchor.current.offset;
    if (top !== viewport.scrollTop) viewport.scrollTop = top;
  }, [items]);

  // Jump to failure picks a row that can be far out of view.
  useEffect(() => {
    const viewport = viewportRef.current;
    const index = items.findIndex(
      (x) => x.kind === "step" && x.step.sequence === selectedSequence,
    );
    if (!viewport || index < 0) return;

    const top = index * ROW_HEIGHT;
    const isInView =
      top >= viewport.scrollTop && top + ROW_HEIGHT <= viewport.scrollTop + viewport.clientHeight;
    if (!isInView) viewport.scrollTop = top;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedSequence]);

  const handleScroll = (top: number) => {
    if (scrollTop === 0 && top > 0) setStepCountAtLeaving(executionSteps.length);
    setScrollTop(top);

    const index = Math.min(items.length - 1, Math.floor(top / ROW_HEIGHT));
    const item = items[index];
    anchor.current = item
      ? {
          sequence: item.kind === "step" ? item.step.sequence : item.firstSequence,
          isFold: item.kind === "fold",
          offset: top - index * ROW_HEIGHT,
        }
      : undefined;
  };

  const toggleFold = (firstSequence: number) => {
    setOpenFolds((open) => {
      const next = new Set(open);
      if (!next.delete(firstSequence)) next.add(firstSequence);
      return next;
    });
  };

  const isAtTop = scrollTop === 0;
  const newCount = isAtTop ? 0 : executionSteps.length - stepCountAtLeaving;
  const isPaused = runState === RunStateEnum.PAUSED;

  let actions: ReactNode = null;
  if (!isEmpty && !isAtTop)
    actions = (
      <Button
        label={newCount > 0 ? `${newCount.toLocaleString()} new · back to latest` : "Back to latest"}
        icon="pi pi-arrow-up"
        size="small"
        rounded
        className="py-1 px-3 text-xs"
        onClick={() => viewportRef.current?.scrollTo({ top: 0 })}
      />
    );
  else if (isLive)
    actions = (
      <span
        role="status"
        className="flex align-items-center gap-2 px-2 text-xs font-medium border-round-3xl"
        style={{ height: 24, color: FOUND_COLOR, background: tint(FOUND_COLOR, 14) }}
      >
        <i
          className="status-pill-pulse"
          style={{ width: 6, height: 6, borderRadius: "50%", background: "currentColor" }}
        />
        Live
      </span>
    );

  return (
    <ExecutionPanelComponent
      title={
        <>
          Execution{" "}
          <span
            className="font-normal"
            style={MUTED}
          >
            · newest first
          </span>
        </>
      }
      actions={actions}
      toolbar={
        <div className="flex flex-wrap align-items-center gap-2">
          <IconField
            iconPosition="left"
            className="flex-1"
            style={{ minWidth: 160 }}
          >
            <InputIcon className="pi pi-search" />
            <InputText
              type="search"
              aria-label="Find a step"
              placeholder="Find a step"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="p-inputtext-sm w-full"
            />
          </IconField>

          <SelectButton
            value={filter}
            options={FILTER_OPTIONS}
            allowEmpty={false}
            onChange={(e) => setFilter(e.value)}
            pt={{ button: { className: "py-1 px-3 text-sm" } }}
          />

          {overview.fatalSequence !== undefined ? (
            <Button
              label="Jump to failure"
              icon="pi pi-arrow-up"
              size="small"
              text
              severity="danger"
              onClick={() => setSelectedSequence(overview.fatalSequence)}
            />
          ) : null}
        </div>
      }
    >
      {isEmpty ? (
        <div className="flex flex-column align-items-center justify-content-center gap-2 p-6 text-center">
          <IconComponent
            name={emptyReason === "error" ? "exclamation-triangle" : "play"}
            size="lg"
            className="opacity-50"
          />
          <LabelComponent
            text={emptyTitle(emptyReason)}
            color="secondary"
          />
          <LabelComponent
            text={emptyMessage ?? emptyDetail(emptyReason)}
            size="sm"
            color="secondary"
          />
        </div>
      ) : (
        <div
          className="flex flex-column flex-1"
          style={{ minHeight: 0 }}
        >
          {isPaused && currentStepName ? <PausedBanner stepName={currentStepName} /> : null}

          {isLive && currentStepName && !isFiltered ? (
            <ExecutingRow
              name={currentStepName}
              isPaused={isPaused}
            />
          ) : null}

          <ColumnHeadings />

          <div
            ref={viewportRef}
            className="flex-1 px-2 pb-2"
            style={{ overflow: "auto", minHeight: 0, overflowAnchor: "none" }}
            onScroll={(e) => handleScroll(e.currentTarget.scrollTop)}
          >
            {items.length === 0 ? (
              <div className="p-4 text-center text-sm" style={MUTED}>
                No step matches.
              </div>
            ) : (
              <VisibleRows
                items={items}
                scrollTop={scrollTop}
                viewportHeight={viewportHeight}
                fatalSequence={overview.fatalSequence}
                selectedSequence={selectedSequence}
                maxDurationMilliseconds={maxDurationMilliseconds}
                onSelect={setSelectedSequence}
                onToggleFold={toggleFold}
              />
            )}
          </div>
        </div>
      )}
    </ExecutionPanelComponent>
  );
}

export default memo(ExecutionStepListComponent);

interface Anchor {
  /** The row's sequence, or a fold's first. */
  sequence: number;
  isFold: boolean;

  /** How far into it the viewport's top sits. */
  offset: number;
}

// A row that has since folded is found in its fold.
function indexOfAnchor(items: ExecutionListItem[], anchor: Anchor): number {
  if (!anchor.isFold) {
    const index = items.findIndex((x) => x.kind === "step" && x.step.sequence === anchor.sequence);
    if (index >= 0) return index;
  }

  return items.findIndex(
    (x) =>
      x.kind === "fold" &&
      (x.firstSequence === anchor.sequence ||
        (!x.isOpen && x.firstSequence <= anchor.sequence && anchor.sequence <= x.lastSequence)),
  );
}

interface VisibleRowsProps {
  items: ExecutionListItem[];
  scrollTop: number;
  viewportHeight: number;
  fatalSequence: number | undefined;
  selectedSequence: number | undefined;
  maxDurationMilliseconds: number;
  onSelect: (sequence: number) => void;
  onToggleFold: (firstSequence: number) => void;
}

function VisibleRows({
  items,
  scrollTop,
  viewportHeight,
  fatalSequence,
  selectedSequence,
  maxDurationMilliseconds,
  onSelect,
  onToggleFold,
}: VisibleRowsProps) {
  const first = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN_ROWS);
  const last = Math.min(
    items.length,
    Math.ceil((scrollTop + viewportHeight) / ROW_HEIGHT) + OVERSCAN_ROWS,
  );

  return (
    <div style={{ position: "relative", height: items.length * ROW_HEIGHT }}>
      <div style={{ position: "absolute", top: first * ROW_HEIGHT, left: 0, right: 0 }}>
        {items.slice(first, last).map((item) =>
          item.kind === "step" ? (
            <ExecutionStepRowComponent
              key={item.step.sequence}
              executionStep={item.step}
              height={ROW_HEIGHT}
              isFatal={item.step.sequence === fatalSequence}
              isSelected={item.step.sequence === selectedSequence}
              maxDurationMilliseconds={maxDurationMilliseconds}
              onSelect={onSelect}
            />
          ) : (
            <ExecutionFoldRowComponent
              key={`fold-${item.firstSequence}`}
              passes={item.passes}
              firstPass={item.firstPass}
              lastPass={item.lastPass}
              stepCount={item.stepCount}
              isOpen={item.isOpen}
              height={ROW_HEIGHT}
              onToggle={() => onToggleFold(item.firstSequence)}
            />
          ),
        )}
      </div>
    </div>
  );
}

function ColumnHeadings() {
  return (
    <div
      aria-hidden="true"
      className="flex align-items-center gap-2 text-xs"
      style={{
        ...MUTED,
        padding: "8px 18px 4px",
        letterSpacing: "0.04em",
        textTransform: "uppercase",
      }}
    >
      <span style={{ width: 48, textAlign: "right" }}>#</span>
      <span style={{ width: 14 }} />
      <span className="flex-1">Step</span>
      <span style={{ width: 58, textAlign: "right" }}>Took</span>
    </div>
  );
}

interface PausedBannerProps {
  stepName: string;
}

function PausedBanner({ stepName }: PausedBannerProps) {
  const keys = useDebuggerKeys();

  const hints = [
    keys.continue ? `${toHotkeyDisplay(keys.continue)} continues` : "",
    keys.stepOver ? `${toHotkeyDisplay(keys.stepOver)} steps over` : "",
    keys.stepInto ? `${toHotkeyDisplay(keys.stepInto)} steps into` : "",
  ].filter((x) => x);

  return (
    <div
      role="status"
      className="flex flex-wrap align-items-center gap-2 mx-3 mt-2 px-3 py-2 border-1 border-round-lg text-sm"
      style={{ borderColor: tint(HANDLED_COLOR, 35), background: tint(HANDLED_COLOR, 8) }}
    >
      <i
        className="pi pi-pause"
        style={{ color: HANDLED_COLOR }}
      />
      <span className="flex-1">
        Paused before <strong style={{ color: HANDLED_COLOR }}>{stepName}</strong>. It has not
        executed yet.
      </span>
      {hints.length > 0 ? (
        <span
          className="text-xs"
          style={MUTED}
        >
          {hints.join(" · ")}
        </span>
      ) : null}
    </div>
  );
}

interface ExecutingRowProps {
  name: string;
  isPaused: boolean;
}

/** The step under way has no row until it finishes, so it is drawn above them. */
function ExecutingRow({ name, isPaused }: ExecutingRowProps) {
  const color = isPaused ? HANDLED_COLOR : FOUND_COLOR;

  return (
    <div
      className="flex align-items-center gap-2 mx-2 mt-2 border-round-lg"
      style={{
        height: ROW_HEIGHT,
        padding: "0 10px",
        whiteSpace: "nowrap",
        fontSize: "0.8rem",
        background: tint(color, 8),
      }}
    >
      <span style={{ width: 48, flex: "none" }} />
      <i
        className={isPaused ? "pi pi-pause" : "pi pi-spinner pi-spin"}
        style={{ color, fontSize: "0.75rem", width: 14, flex: "none" }}
      />
      <span
        className="font-semibold"
        style={{ minWidth: 0, overflow: "hidden", textOverflow: "ellipsis" }}
      >
        {name}
      </span>
      <span style={{ ...chipStyle(color), flex: "none" }}>{isPaused ? "next" : "executing"}</span>
      <span
        style={{ ...MUTED, marginLeft: "auto", width: 58, textAlign: "right", flex: "none" }}
      >
        …
      </span>
    </div>
  );
}
