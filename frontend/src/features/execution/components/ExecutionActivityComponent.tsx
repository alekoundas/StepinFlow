import { memo } from "react";

import { toElapsed } from "@/shared/utils/clock";
import { tint } from "@/shared/models/flow-step-catalog";
import { CARD, FATAL_COLOR, FOUND_COLOR, MONO, MUTED } from "@/features/execution/execution-format";
import type { Activity } from "@/features/execution/hooks/use-execution-overview";

const BARS_HEIGHT = 52;
const TALLEST_BAR = 40;

interface Props {
  activity: Activity;

  /** Still going, so the last bar is the one filling up. */
  isLive: boolean;
}

/**
 * How busy the execution was over its whole length, and where it found something.
 *
 * A list newest first shows the last minutes; this shows the hours, so a loop that went quiet an
 * hour ago, or found something every ten minutes, is seen without scrolling for it.
 */
function ExecutionActivityComponent({ activity, isLive }: Props) {
  const { bars, barMilliseconds } = activity;
  if (bars.length < 2) return null;

  const most = bars.reduce((max, x) => Math.max(max, x.steps), 1);
  const span = bars.length * barMilliseconds;
  const barsThatFound = bars.filter((x) => x.found > 0).length;
  const hasFatal = bars.some((x) => x.hasFatal);

  const label =
    `Steps ${per(barMilliseconds)} over ${toElapsed(span)}. ` +
    `${barsThatFound} of ${bars.length} found something` +
    (hasFatal ? ", and the execution ended on a failure." : ".");

  return (
    <section
      aria-labelledby="execution-activity"
      className="flex flex-column gap-2 px-3 pt-2 pb-2 border-1 border-round-xl"
      style={CARD}
    >
      <div className="flex flex-wrap align-items-baseline justify-content-between gap-2">
        <h2
          id="execution-activity"
          className="m-0 text-sm font-semibold"
        >
          Activity <span className="font-normal" style={MUTED}>· steps {per(barMilliseconds)}</span>
        </h2>

        <span className="flex align-items-center gap-3 text-xs" style={MUTED}>
          <span className="flex align-items-center gap-1">
            <span style={{ width: 8, height: 8, borderRadius: "50%", background: FOUND_COLOR }} />
            found something
          </span>
          {hasFatal ? (
            <span className="flex align-items-center gap-1">
              <span style={{ width: 8, height: 8, borderRadius: 2, background: FATAL_COLOR }} />
              ended the execution
            </span>
          ) : null}
        </span>
      </div>

      <div
        role="img"
        aria-label={label}
        className="flex align-items-end"
        style={{ gap: 2, height: BARS_HEIGHT }}
      >
        {bars.map((bar, i) => {
          const isNow = isLive && i === bars.length - 1;
          let mark = "transparent";
          if (bar.hasFatal) mark = FATAL_COLOR;
          else if (bar.found > 0) mark = FOUND_COLOR;

          return (
            <span
              key={i}
              title={barTitle(bar.steps, bar.found, bar.hasFatal, i * barMilliseconds)}
              className="flex flex-column align-items-center justify-content-end"
              style={{ flex: "1 1 0", minWidth: 0, height: BARS_HEIGHT, gap: 3 }}
            >
              <span
                style={{ width: 6, height: 6, borderRadius: bar.hasFatal ? 1 : "50%", background: mark }}
              />
              <span
                style={{
                  width: "100%",
                  height: bar.steps > 0 ? Math.max(2, Math.round((bar.steps / most) * TALLEST_BAR)) : 0,
                  borderRadius: 2,
                  background: isNow ? FOUND_COLOR : tint(FOUND_COLOR, 30),
                }}
              />
            </span>
          );
        })}
      </div>

      <div
        aria-hidden="true"
        className="flex justify-content-between text-xs"
        style={{ ...MUTED, fontFamily: MONO }}
      >
        {[0, 0.25, 0.5, 0.75].map((x) => (
          <span key={x}>{toElapsed(span * x)}</span>
        ))}
        <span style={isLive ? { color: FOUND_COLOR } : undefined}>
          {isLive ? `now ${toElapsed(span)}` : toElapsed(span)}
        </span>
      </div>
    </section>
  );
}

// Redrawn when a batch changes the bars, not when the page draws for anything else.
export default memo(ExecutionActivityComponent);

function per(milliseconds: number): string {
  if (milliseconds === 1_000) return "a second";
  if (milliseconds === 60_000) return "a minute";
  if (milliseconds === 3_600_000) return "an hour";
  if (milliseconds < 60_000) return `every ${milliseconds / 1000} seconds`;
  if (milliseconds < 3_600_000) return `every ${milliseconds / 60_000} minutes`;
  return `every ${milliseconds / 3_600_000} hours`;
}

function barTitle(steps: number, found: number, hasFatal: boolean, from: number): string {
  let title = `${toElapsed(from)} · ${steps.toLocaleString()} steps`;
  if (found > 0) title += `, ${found.toLocaleString()} found`;
  if (hasFatal) title += ", ended the execution";
  return title;
}
