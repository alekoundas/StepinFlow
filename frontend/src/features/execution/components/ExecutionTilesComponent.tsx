import { useEffect, useState } from "react";

import { toElapsed } from "@/shared/utils/clock";
import {
  CARD,
  FATAL_COLOR,
  FOUND_COLOR,
  HANDLED_COLOR,
  MUTED,
} from "@/features/execution/execution-format";
import type { ExecutionOverview } from "@/features/execution/hooks/use-execution-overview";

interface Props {
  overview: ExecutionOverview;
  stepCount: number;

  /** Still going, so the elapsed time counts up. */
  isLive: boolean;
}

/**
 * The shape of the execution before any scrolling.
 *
 * The count that matters is the split between failures: hours of handled failures and no fatal one
 * is a bot loop doing its job, and it reads as a disaster in a list of red rows.
 */
export default function ExecutionTilesComponent({ overview, stepCount, isLive }: Props) {
  const now = useClock(isLive);

  if (stepCount === 0 || overview.startedAt === undefined || overview.endedAt === undefined)
    return null;

  const elapsed = Math.max(0, (isLive ? Math.max(now, overview.endedAt) : overview.endedAt) - overview.startedAt);
  const perSecond = elapsed >= 1000 ? stepCount / (elapsed / 1000) : undefined;
  const loop = overview.loop;

  const tiles: Tile[] = [
    {
      label: "Elapsed",
      value: toElapsed(elapsed),
      note: isLive ? "and counting" : "from first step to last",
    },
    {
      label: "Steps",
      value: stepCount.toLocaleString(),
      note: perSecond ? `${perSecond.toFixed(1)} a second` : "",
    },
  ];

  if (loop)
    tiles.push({
      label: "Loop",
      value: `pass ${loop.pass.toLocaleString()}`,
      note: loop.name,
    });

  if (loop)
    tiles.push({
      label: "Found something",
      value: `${loop.passesThatFound.toLocaleString()} ${loop.passesThatFound === 1 ? "pass" : "passes"}`,
      note: loop.lastPassThatFound
        ? `${overview.foundCount.toLocaleString()} found, last in pass ${loop.lastPassThatFound.toLocaleString()}`
        : "nothing yet",
      color: FOUND_COLOR,
    });
  else
    tiles.push({
      label: "Found something",
      value: overview.foundCount.toLocaleString(),
      note: "checks that succeeded",
      color: FOUND_COLOR,
    });

  tiles.push({
    label: "Handled failures",
    value: overview.handledCount.toLocaleString(),
    note: overview.fatalSequence !== undefined ? "and 1 ended the execution" : "checks that found nothing and went on",
    color: HANDLED_COLOR,
    noteColor: overview.fatalSequence !== undefined ? FATAL_COLOR : undefined,
  });

  return (
    <section
      aria-label="So far"
      style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", gap: 12 }}
    >
      {tiles.map((tile) => (
        <div
          key={tile.label}
          className="flex flex-column gap-1 px-3 py-2 border-1 border-round-xl min-w-0"
          style={CARD}
        >
          <span
            className="text-xs"
            style={MUTED}
          >
            {tile.label}
          </span>
          <span
            className="text-2xl font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis"
            style={{ color: tile.color, fontVariantNumeric: "tabular-nums" }}
          >
            {tile.value}
          </span>
          <span
            className="text-xs white-space-nowrap overflow-hidden text-overflow-ellipsis"
            style={{ color: tile.noteColor ?? "var(--text-color-secondary)" }}
            title={tile.note}
          >
            {tile.note || " "}
          </span>
        </div>
      ))}
    </section>
  );
}

interface Tile {
  label: string;
  value: string;
  note: string;
  color?: string;
  noteColor?: string;
}

/** The time now, a second at a time while ticking. */
function useClock(isTicking: boolean): number {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!isTicking) return;

    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [isTicking]);

  return now;
}
