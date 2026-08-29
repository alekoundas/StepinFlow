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
  return (
    <span className={classNames("status-pill", `status-pill-${severity}`, className)}>
      <i className={classNames("status-pill-dot", { "status-pill-pulse": pulse })} />
      {text}
    </span>
  );
}
