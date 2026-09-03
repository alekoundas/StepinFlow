import { classNames } from "primereact/utils";

export type StatusPillSeverity =
  | "neutral"
  | "running"
  | "paused"
  | "success"
  | "danger";

interface Props {
  text: string;
  severity?: StatusPillSeverity;

  /** Slowly fades the dot, for a state that is still moving. */
  pulse?: boolean;
  className?: string;
}

/**
 * A dot and a word. Used wherever a run's state has to read at a glance - the execution toolbar
 * and every row of the history table - so the same state never looks like two different things.
 */
export default function StatusPillComponent({
  text,
  severity = "neutral",
  pulse = false,
  className,
}: Props) {
  const colour = severityColour(severity);

  return (
    <span
      className={className}
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: "0.45rem",
        padding: "0.25rem 0.7rem",
        borderRadius: 100,
        border: `1px solid ${colour ?? "var(--surface-border)"}`,
        background: "var(--surface-card)",
        color: colour ?? "var(--text-color-secondary)",
        fontSize: "0.75rem",
        whiteSpace: "nowrap",
      }}
    >
      {/* The breathe animation is keyframes, which is the one thing a style attribute cannot hold. */}
      <i
        className={classNames({ "status-pill-pulse": pulse })}
        style={{
          width: 7,
          height: 7,
          borderRadius: "50%",
          background: "currentColor",
          opacity: 0.9,
        }}
      />
      {text}
    </span>
  );
}

function severityColour(severity: StatusPillSeverity): string | undefined {
  switch (severity) {
    case "running":
      return "var(--blue-500)";
    case "paused":
      return "var(--yellow-500)";
    case "success":
      return "var(--green-500)";
    case "danger":
      return "var(--red-500)";
    default:
      return undefined;
  }
}
