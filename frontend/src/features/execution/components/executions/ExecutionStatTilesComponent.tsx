import { Skeleton } from "primereact/skeleton";

import { dayAndTime } from "@/shared/utils/day-and-time";
import { toTook } from "@/shared/utils/clock";
import type { ExecutionStatsDto } from "@/shared/models/database/execution-stats-dto";
import { useExecutionStats } from "@/features/execution/hooks/use-execution";
import { MUTED } from "@/features/execution/execution-format";
import { completedPercentOf, dayOf } from "@/features/execution/components/executions/executions-format";

const DAYS = 7;

interface Tile {
  value: string;
  label: string;
  note: string;
  dotColor?: string;
}

/**
 * The last week in four numbers. For every flow: how many executed, how often they completed, how
 * many failed and how many were stopped. For one flow, how long it usually takes and when it last
 * failed say more than the last two.
 */
export default function ExecutionStatTilesComponent({ flowId }: { flowId: number | null }) {
  const { data: stats } = useExecutionStats(flowId, DAYS);

  if (!stats)
    return (
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", gap: 10 }}>
        <Skeleton height="5rem" />
        <Skeleton height="5rem" />
        <Skeleton height="5rem" />
        <Skeleton height="5rem" />
      </div>
    );

  const tiles = flowId === null ? allFlowsTiles(stats) : oneFlowTiles(stats);

  return (
    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", gap: 10 }}>
      {tiles.map((tile) => (
        <div
          key={tile.label}
          className="flex flex-column gap-1 p-3 border-round-xl"
          style={{ background: "var(--surface-ground)" }}
        >
          <span
            className="text-2xl font-semibold"
            style={{ letterSpacing: "-0.02em" }}
          >
            {tile.value}
          </span>
          <span
            className="flex align-items-center gap-2 text-xs white-space-nowrap"
            style={MUTED}
          >
            {tile.dotColor && (
              <span
                className="flex-none border-circle"
                style={{ width: 7, height: 7, background: tile.dotColor }}
              />
            )}
            {tile.label}
          </span>
          <span
            className="text-xs"
            style={{ color: "var(--text-color-secondary)", opacity: 0.75 }}
          >
            {tile.note}
          </span>
        </div>
      ))}
    </div>
  );
}

const completedTile = (stats: ExecutionStatsDto): Tile => {
  const failed = stats.failed + stats.errored;
  const percent = completedPercentOf(stats.completed, failed);

  return {
    value: percent === null ? "—" : `${percent}%`,
    label: "Completed",
    note: percent === null ? "none ended with a verdict" : `${stats.completed} of the ${stats.completed + failed} with a verdict`,
    dotColor: "var(--green-400)",
  };
};

const allFlowsTiles = (stats: ExecutionStatsDto): Tile[] => {
  const stopped = stats.stopped + stats.abandoned;

  let lastFailure = "none failed";
  if (stats.lastFailureOn) lastFailure = `last on ${dayAndTime(stats.lastFailureOn)}`;

  let abandoned = "none abandoned";
  if (stats.abandoned > 0) abandoned = `${stats.abandoned} of them abandoned`;

  return [
    { value: stats.total.toLocaleString(), label: "Executions", note: `in the last ${DAYS} days` },
    completedTile(stats),
    { value: (stats.failed + stats.errored).toLocaleString(), label: "Failed", note: lastFailure, dotColor: "var(--red-400)" },
    { value: stopped.toLocaleString(), label: "Stopped", note: abandoned, dotColor: "var(--bluegray-400)" },
  ];
};

const oneFlowTiles = (stats: ExecutionStatsDto): Tile[] => {
  let usual = "—";
  if (stats.usualDurationMilliseconds != null) usual = toTook(stats.usualDurationMilliseconds);

  let lastFailure = "—";
  if (stats.lastFailureOn) lastFailure = dayOf(stats.lastFailureOn);

  return [
    { value: stats.total.toLocaleString(), label: "Executions", note: `in the last ${DAYS} days` },
    completedTile(stats),
    { value: usual, label: "Usually takes", note: "when it completes" },
    { value: lastFailure, label: "Last failure", note: stats.lastFailureOn ? dayAndTime(stats.lastFailureOn) : "none in that week" },
  ];
};
