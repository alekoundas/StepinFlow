import { useState, type KeyboardEvent, type MouseEvent } from "react";
import { Button } from "primereact/button";
import { Skeleton } from "primereact/skeleton";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";
import type { ExecutionStatsDto } from "@/shared/models/database/execution-stats-dto";
import { toTook } from "@/shared/utils/clock";
import ExecutedVersionComponent from "@/features/repository/components/ExecutedVersionComponent";
import { useAiStatus } from "@/features/ai/hooks/use-ai";
import { useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { MONO, MUTED } from "@/features/execution/execution-format";
import { useExecutionFeed, useExecutionStats } from "@/features/execution/hooks/use-execution";
import {
  SURFACE,
  dayLabelOf,
  outcomeLookOf,
  stepsText,
  timeOf,
} from "@/features/execution/components/executions/executions-format";

type Filter = "all" | "failed" | "completed" | "stopped" | "inconclusive";

const STATUSES: Record<Filter, ExecutionStatusEnum[]> = {
  all: [],
  failed: [ExecutionStatusEnum.FAILED, ExecutionStatusEnum.ERRORED],
  completed: [ExecutionStatusEnum.COMPLETED],
  stopped: [ExecutionStatusEnum.STOPPED, ExecutionStatusEnum.ABANDONED],
  inconclusive: [ExecutionStatusEnum.INCONCLUSIVE],
};

// The status, its number, who and how it went, when, how long, how many steps, the version, what it
// kept, delete.
const ROW_COLUMNS = "24px 56px minmax(0, 1fr) 52px 72px 116px minmax(0, 150px) 20px 20px 12px";

interface Props {
  flowId: number | null;
  live: { executionId: number; stepCount: number } | null;

  /** Every flow's feed names each execution's flow; one flow's would only repeat it. */
  showsFlowName: boolean;
  onOpen: (execution: ExecutionFeedItemDto) => void;
  onDelete: (execution: ExecutionFeedItemDto) => void;
}

/** Executions newest first, by day, filtered by how they ended. Older ones come a page at a time. */
export default function ExecutionFeedComponent({ flowId, live, showsFlowName, onOpen, onDelete }: Props) {
  const [filter, setFilter] = useState<Filter>("all");
  const { data: counts } = useExecutionStats(flowId, null);
  const { data, isPending, hasNextPage, fetchNextPage, isFetchingNextPage } = useExecutionFeed(flowId, STATUSES[filter]);

  const executions = data?.pages.flat() ?? [];

  let emptyMessage = "Nothing executed yet.";
  if (filter !== "all") emptyMessage = "No execution ended this way.";

  return (
    <div className="flex flex-column gap-3">
      <div
        role="group"
        aria-label="Filter"
        className="flex flex-wrap gap-2"
      >
        {chipsOf(counts).map((chip) => (
          <Button
            key={chip.id}
            type="button"
            label={chip.count === undefined ? chip.label : `${chip.label} ${chip.count.toLocaleString()}`}
            size="small"
            rounded
            outlined
            severity={chip.id === filter ? undefined : "secondary"}
            aria-pressed={chip.id === filter}
            onClick={() => setFilter(chip.id)}
          />
        ))}
      </div>

      {isPending && (
        <div className="flex flex-column gap-2">
          <Skeleton height="2.75rem" />
          <Skeleton height="2.75rem" />
          <Skeleton height="2.75rem" />
        </div>
      )}

      {!isPending && executions.length === 0 && (
        <span
          className="text-sm p-3"
          style={MUTED}
        >
          {emptyMessage}
        </span>
      )}

      {executions.length > 0 && (
        <div
          className="border-1 border-round-xl overflow-hidden"
          style={SURFACE}
        >
          {daysOf(executions).map((day) => (
            <section
              key={day.label}
              aria-label={day.label}
            >
              <div
                className="flex align-items-center gap-2 px-3 pt-3 pb-1 text-xs font-semibold uppercase"
                style={{ ...MUTED, letterSpacing: "0.08em" }}
              >
                {day.label}
                <span
                  className="flex-1"
                  style={{ height: 1, background: "var(--surface-border)" }}
                />
              </div>

              {day.executions.map((execution, index) => (
                <ExecutionRowComponent
                  key={execution.id}
                  execution={execution}
                  showsFlowName={showsFlowName}
                  liveStepCount={execution.id === live?.executionId ? live.stepCount : null}
                  hasTopLine={index > 0}
                  onOpen={() => onOpen(execution)}
                  onDelete={() => onDelete(execution)}
                />
              ))}
            </section>
          ))}
        </div>
      )}

      {hasNextPage && (
        <Button
          type="button"
          label="Show older executions"
          icon="pi pi-angle-down"
          size="small"
          text
          loading={isFetchingNextPage}
          className="align-self-center"
          onClick={() => void fetchNextPage()}
        />
      )}
    </div>
  );
}

const chipsOf = (counts: ExecutionStatsDto | undefined): { id: Filter; label: string; count: number | undefined }[] => {
  const chips: { id: Filter; label: string; count: number | undefined }[] = [
    { id: "all", label: "All", count: counts?.total },
    { id: "failed", label: "Failed", count: counts && counts.failed + counts.errored },
    { id: "completed", label: "Completed", count: counts?.completed },
    { id: "stopped", label: "Stopped", count: counts && counts.stopped + counts.abandoned },
  ];

  if (counts && counts.inconclusive > 0) chips.push({ id: "inconclusive", label: "Inconclusive", count: counts.inconclusive });

  return chips;
};

const daysOf = (executions: ExecutionFeedItemDto[]): { label: string; executions: ExecutionFeedItemDto[] }[] => {
  const days: { label: string; executions: ExecutionFeedItemDto[] }[] = [];

  for (const execution of executions) {
    const label = dayLabelOf(execution.createdOn);
    const last = days[days.length - 1];

    if (last?.label === label) last.executions.push(execution);
    else days.push({ label, executions: [execution] });
  }

  return days;
};

interface RowProps {
  execution: ExecutionFeedItemDto;
  showsFlowName: boolean;

  /** The steps so far, while it executes. Null once it has ended. */
  liveStepCount: number | null;
  hasTopLine: boolean;
  onOpen: () => void;
  onDelete: () => void;
}

function ExecutionRowComponent({ execution, showsFlowName, liveStepCount, hasTopLine, onOpen, onDelete }: RowProps) {
  const [isHovered, setIsHovered] = useState(false);
  const look = outcomeLookOf(execution.status, execution.errorStepName);

  const isLive = liveStepCount !== null;
  const isNotKept = execution.historyLevel === ExecutionHistoryLevelEnum.NONE && execution.stepCount === 0;

  let took = "—";
  if (execution.completedAt)
    took = toTook(new Date(execution.completedAt).getTime() - new Date(execution.createdOn).getTime());

  let steps = stepsText(execution.stepCount);
  if (isNotKept) steps = "—";
  if (isLive) steps = stepsText(liveStepCount);

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) return;
    if (event.key !== "Enter" && event.key !== " ") return;

    event.preventDefault();
    onOpen();
  };

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onOpen}
      onKeyDown={onKeyDown}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className="px-3 py-2 text-sm cursor-pointer"
      style={{
        display: "grid",
        gridTemplateColumns: ROW_COLUMNS,
        alignItems: "center",
        columnGap: 8,
        rowGap: 2,
        borderTop: hasTopLine ? "1px solid var(--surface-border)" : "none",
        background: isHovered ? "color-mix(in srgb, var(--text-color) 5%, transparent)" : "transparent",
      }}
    >
      <span
        className="flex align-items-center justify-content-center border-circle"
        style={{ width: 22, height: 22, color: look.color, background: `color-mix(in srgb, ${look.color} 16%, transparent)` }}
      >
        <i
          className={look.icon}
          style={{ fontSize: 11 }}
          aria-hidden="true"
        />
      </span>

      <span
        className="text-xs"
        style={{ ...MUTED, fontFamily: MONO }}
      >
        #{execution.id}
      </span>

      <span className="flex align-items-baseline gap-2 min-w-0 white-space-nowrap overflow-hidden">
        {showsFlowName && (
          <span
            className="font-semibold overflow-hidden text-overflow-ellipsis"
            title={execution.flowName}
          >
            {execution.flowName}
          </span>
        )}
        <span
          className="overflow-hidden text-overflow-ellipsis"
          style={{ color: look.isWithoutVerdict ? "var(--text-color-secondary)" : look.color }}
        >
          {look.text}
        </span>
      </span>

      <span
        className="text-right"
        style={MUTED}
      >
        {timeOf(execution.createdOn)}
      </span>
      <span
        className="text-right"
        style={MUTED}
      >
        {took}
      </span>
      <span
        className="text-right white-space-nowrap"
        style={MUTED}
      >
        {steps}
      </span>

      <span className="flex justify-content-end overflow-hidden">
        <ExecutedVersionComponent
          commitSha={execution.commitSha}
          hasUncommittedChanges={execution.hasUncommittedChanges}
        />
      </span>

      <KeptComponent execution={execution} />

      {isLive ? <span /> : <DeleteComponent onDelete={onDelete} />}

      <i
        className="pi pi-angle-right text-xs"
        style={MUTED}
        aria-hidden="true"
      />

      {execution.errorMessage && <WhyComponent execution={execution} />}
    </div>
  );
}

/** What it kept, as one icon. Screenshots open their folder. */
function KeptComponent({ execution }: { execution: ExecutionFeedItemDto }) {
  const path = execution.screenshotFolderPath;

  if (path) {
    const open = (event: MouseEvent) => {
      event.stopPropagation();
      void ElectronApiService.files.showInFolder(path);
    };

    return (
      <button
        type="button"
        onClick={open}
        title="Steps and screenshots kept. Opens their folder."
        aria-label="Open the screenshots folder"
        className="flex align-items-center justify-content-center p-0 border-none cursor-pointer"
        style={{ background: "transparent", ...MUTED }}
      >
        <i
          className="pi pi-camera text-sm"
          aria-hidden="true"
        />
      </button>
    );
  }

  let icon = "pi pi-list";
  let title = "Its steps were kept, and no screenshot.";
  if (execution.historyLevel === ExecutionHistoryLevelEnum.NONE) {
    icon = "pi pi-eye-slash";
    title = "Nothing was kept but its outcome.";
  }

  return (
    <i
      className={`${icon} text-sm`}
      style={{ ...MUTED, opacity: 0.7 }}
      title={title}
      aria-label={title}
    />
  );
}

/** Asks first. The one executing now has none: it is stopped before it can be deleted. */
function DeleteComponent({ onDelete }: { onDelete: () => void }) {
  const remove = (event: MouseEvent) => {
    event.stopPropagation();
    onDelete();
  };

  return (
    <button
      type="button"
      onClick={remove}
      title="Delete this execution"
      aria-label="Delete this execution"
      className="flex align-items-center justify-content-center p-0 border-none cursor-pointer"
      style={{ background: "transparent", ...MUTED }}
    >
      <i
        className="pi pi-trash text-sm"
        aria-hidden="true"
      />
    </button>
  );
}

/** What ended it, under the row, with Explain when the AI is on. */
function WhyComponent({ execution }: { execution: ExecutionFeedItemDto }) {
  const { data: isAiOn } = useAiStatus();
  const { askInNewConversation } = useAskAi();

  const look = outcomeLookOf(execution.status, execution.errorStepName);

  const explain = (event: MouseEvent) => {
    event.stopPropagation();
    askInNewConversation("Explain this execution: what happened, and what should I change?", {
      executionId: execution.id,
      sendScreenshots: true,
    });
  };

  return (
    <span
      className="flex align-items-center gap-2 text-xs min-w-0"
      style={{ gridColumn: "3 / -1", color: look.isWithoutVerdict ? "var(--text-color-secondary)" : look.color }}
    >
      <i
        className="pi pi-exclamation-circle"
        aria-hidden="true"
      />
      <span
        className="white-space-nowrap overflow-hidden text-overflow-ellipsis"
        title={execution.errorMessage}
      >
        {execution.errorMessage}
      </span>
      {isAiOn && (
        <button
          type="button"
          onClick={explain}
          className="flex-none p-0 border-none cursor-pointer text-xs"
          style={{ background: "transparent", color: "var(--primary-color)", font: "inherit" }}
        >
          Explain
        </button>
      )}
    </span>
  );
}
