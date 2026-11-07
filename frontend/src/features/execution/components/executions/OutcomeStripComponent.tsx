import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import { executionStatusText } from "@/shared/utils/execution-status";
import { outcomeLookOf } from "@/features/execution/components/executions/executions-format";

/**
 * The last few executions as a row of bars, oldest first. A flow that has started failing looks
 * different from one that always has. One stopped without a verdict is a shorter, grey bar.
 */
export default function OutcomeStripComponent({ outcomes }: { outcomes: ExecutionStatusEnum[] }) {
  return (
    <span
      className="inline-flex align-items-end flex-none"
      style={{ gap: 2, height: 14 }}
      aria-hidden="true"
    >
      {outcomes.map((outcome, index) => {
        const look = outcomeLookOf(outcome);

        return (
          <span
            key={index}
            title={executionStatusText(outcome)}
            style={{
              width: 5,
              height: look.isWithoutVerdict ? 8 : 14,
              borderRadius: 1.5,
              background: look.isWithoutVerdict ? `color-mix(in srgb, ${look.color} 70%, transparent)` : look.color,
            }}
          />
        );
      })}
    </span>
  );
}
