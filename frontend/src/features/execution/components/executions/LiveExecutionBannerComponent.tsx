import { useEffect, useState } from "react";
import { Button } from "primereact/button";

import { Clock } from "@/shared/utils/clock";
import ExecutedVersionComponent from "@/features/repository/components/ExecutedVersionComponent";
import { ExecutionFormat } from "@/features/execution/execution-format";
import { ExecutionsFormat } from "@/features/execution/components/executions/executions-format";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";

const LIVE_COLOR = "var(--blue-400)";

interface Props {
  flowName: string;
  executionId: number;
  stepCount: number;

  /** Its row, for when it started. Null until it has been read. */
  execution: ExecutionFeedItemDto | null;
  onView: () => void;
}

/** What is executing now, above everything that already has. */
export default function LiveExecutionBannerComponent({ flowName, executionId, stepCount, execution, onView }: Props) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  const facts: string[] = [];
  if (execution) facts.push(`Started ${ExecutionsFormat.timeOf(execution.createdOn)}`);
  if (execution) facts.push(Clock.toTook(now - new Date(execution.createdOn).getTime()));
  facts.push(`${ExecutionsFormat.stepsText(stepCount)} so far`);

  return (
    <section
      aria-label="Executing now"
      className="flex align-items-center gap-3 px-3 py-2 border-1 border-round-xl"
      style={{
        borderColor: `color-mix(in srgb, ${LIVE_COLOR} 45%, transparent)`,
        background: `color-mix(in srgb, ${LIVE_COLOR} 10%, transparent)`,
      }}
    >
      <span
        className="flex-none border-circle status-pill-pulse"
        style={{
          width: 10,
          height: 10,
          background: LIVE_COLOR,
          boxShadow: `0 0 0 5px color-mix(in srgb, ${LIVE_COLOR} 25%, transparent)`,
        }}
      />

      <div className="flex flex-column flex-1 min-w-0">
        <span className="flex align-items-baseline gap-2">
          <span>
            <span className="font-semibold">{flowName}</span> is executing
          </span>
          <span
            className="text-xs"
            style={ExecutionFormat.MUTED}
          >
            #{executionId}
          </span>
        </span>
        <span
          className="flex align-items-center flex-wrap gap-2 text-sm"
          style={ExecutionFormat.MUTED}
        >
          {facts.join(" · ")}
          {execution && (
            <ExecutedVersionComponent
              commitSha={null}
              hasUncommittedChanges={execution.hasUncommittedChanges}
            />
          )}
        </span>
      </div>

      <Button
        type="button"
        label="View"
        icon="pi pi-eye"
        size="small"
        onClick={onView}
      />
    </section>
  );
}
