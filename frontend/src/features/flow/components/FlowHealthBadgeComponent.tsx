import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";

interface Props {
  health: FlowHealthDto | undefined;
  isEmpty: boolean;
}

/**
 * Whether the flow would execute. The question you actually have when looking at a list of them.
 *
 * Nothing is shown while the counts are still loading rather than a placeholder that flickers
 * into a different answer a moment later.
 */
export default function FlowHealthBadgeComponent({ health, isEmpty }: Props) {
  if (isEmpty)
    return (
      <HealthComponent
        color="var(--surface-400)"
        text="No steps"
      />
    );

  if (!health) return null;

  if (health.errorCount > 0)
    return (
      <HealthComponent
        color="var(--red-400)"
        text={plural(health.errorCount, "error")}
      />
    );

  if (health.warningCount > 0)
    return (
      <HealthComponent
        color="var(--yellow-500)"
        text={plural(health.warningCount, "warning")}
      />
    );

  return (
    <HealthComponent
      color="var(--green-400)"
      text="No issues"
    />
  );
}

function HealthComponent({ color, text }: { color: string; text: string }) {
  return (
    <span className="inline-flex align-items-center gap-2 text-color-secondary white-space-nowrap">
      <span
        className="border-circle flex-none"
        style={{ width: 8, height: 8, background: color }}
      />
      {text}
    </span>
  );
}

const plural = (count: number, noun: string) =>
  `${count} ${noun}${count === 1 ? "" : "s"}`;
