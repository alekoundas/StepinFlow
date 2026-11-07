import { useState, type ReactNode } from "react";
import { Skeleton } from "primereact/skeleton";

import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import { MUTED } from "@/features/execution/execution-format";
import OutcomeStripComponent from "@/features/execution/components/executions/OutcomeStripComponent";
import { SURFACE, completedPercentOf, outcomeLookOf } from "@/features/execution/components/executions/executions-format";

interface Props {
  summaries: FlowExecutionSummaryDto[] | undefined;
  allCount: number | undefined;
  selectedFlowId: number | null;
  executingFlowId: number | null;
  onSelect: (flowId: number | null) => void;
}

/**
 * All flows, then each flow by when it last executed: how its last few went and how often it
 * completed. A sub-flow executes inside the flow that calls it, so one is listed only once it has
 * executions of its own.
 */
export default function ExecutionFlowListComponent({
  summaries,
  allCount,
  selectedFlowId,
  executingFlowId,
  onSelect,
}: Props) {
  const flows = (summaries ?? [])
    .filter((x) => !x.isSubFlow || x.runCount > 0)
    .sort((a, b) => (b.lastRunOn ?? "").localeCompare(a.lastRunOn ?? ""));

  return (
    <nav
      aria-label="Flows"
      className="flex flex-column gap-1 p-2 border-1 border-round-xl"
      style={{ ...SURFACE, position: "sticky", top: 16 }}
    >
      <span
        className="px-2 pt-1 pb-2 text-xs font-semibold uppercase"
        style={{ ...MUTED, letterSpacing: "0.08em" }}
      >
        Flows
      </span>

      <FlowRowComponent
        isSelected={selectedFlowId === null}
        onClick={() => onSelect(null)}
        dot={
          <i
            className="pi pi-th-large text-xs"
            style={{ color: "var(--primary-color)" }}
            aria-hidden="true"
          />
        }
        name="All flows"
        count={allCount}
      />

      {!summaries && (
        <div className="flex flex-column gap-2 p-2">
          <Skeleton height="2.5rem" />
          <Skeleton height="2.5rem" />
          <Skeleton height="2.5rem" />
        </div>
      )}

      {flows.map((flow) => (
        <FlowRowComponent
          key={flow.flowId}
          isSelected={selectedFlowId === flow.flowId}
          onClick={() => onSelect(flow.flowId)}
          dot={<DotComponent color={dotColorOf(flow, executingFlowId)} />}
          name={flow.flowName}
          count={flow.runCount}
        >
          <span className="flex align-items-center justify-content-between gap-2">
            <OutcomeStripComponent outcomes={flow.recentOutcomes} />
            <span
              className="text-xs white-space-nowrap"
              style={MUTED}
            >
              {rateText(flow)}
            </span>
          </span>
        </FlowRowComponent>
      ))}

      <span
        className="px-2 pt-2 pb-1 text-xs line-height-3"
        style={MUTED}
      >
        Sub-flows execute inside the flow that calls them, so one is listed only once it has executions of its own.
      </span>
    </nav>
  );
}

const dotColorOf = (flow: FlowExecutionSummaryDto, executingFlowId: number | null): string => {
  if (flow.flowId === executingFlowId) return "var(--blue-400)";
  if (!flow.lastStatus) return "var(--surface-400)";

  return outcomeLookOf(flow.lastStatus).color;
};

const rateText = (flow: FlowExecutionSummaryDto): string => {
  if (flow.runCount === 0) return "Not executed yet";

  const percent = completedPercentOf(flow.completedCount, flow.failedCount);
  if (percent === null) return "no verdict";

  return `${percent}% completed`;
};

interface FlowRowProps {
  isSelected: boolean;
  onClick: () => void;
  dot: ReactNode;
  name: string;
  count: number | undefined;
  children?: ReactNode;
}

function FlowRowComponent({ isSelected, onClick, dot, name, count, children }: FlowRowProps) {
  const [isHovered, setIsHovered] = useState(false);

  let background = "transparent";
  if (isHovered) background = "color-mix(in srgb, var(--text-color) 6%, transparent)";
  if (isSelected) background = "color-mix(in srgb, var(--primary-color) 16%, transparent)";

  return (
    <button
      type="button"
      onClick={onClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      aria-pressed={isSelected}
      className="flex flex-column gap-2 w-full px-2 py-2 border-none border-round-lg text-left cursor-pointer"
      style={{
        background,
        boxShadow: isSelected ? "inset 3px 0 0 var(--primary-color)" : "none",
        color: "var(--text-color)",
        font: "inherit",
      }}
    >
      <span className="flex align-items-center gap-2 w-full">
        {dot}
        <span
          className="flex-1 min-w-0 text-sm font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis"
          title={name}
        >
          {name}
        </span>
        {count !== undefined && (
          <span
            className="text-xs"
            style={MUTED}
          >
            {count.toLocaleString()}
          </span>
        )}
      </span>
      {children && <span className="w-full">{children}</span>}
    </button>
  );
}

function DotComponent({ color }: { color: string }) {
  return (
    <span
      className="flex-none border-circle"
      style={{ width: 7, height: 7, background: color }}
    />
  );
}
