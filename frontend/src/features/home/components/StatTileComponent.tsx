import { MUTED } from "@/features/home/home-format";

interface Props {
  value: string | number;
  label: string;

  /** Beside the label, so the value itself stays in the text colour. */
  dotColor?: string;
}

/** A value large, with what it is under it. */
export default function StatTileComponent({ value, label, dotColor }: Props) {
  return (
    <div
      className="flex flex-column gap-1 flex-1 min-w-0 p-3 border-round-xl"
      style={{ background: "var(--surface-ground)" }}
    >
      <span
        className="text-2xl font-semibold"
        style={{ letterSpacing: "-0.02em" }}
      >
        {value}
      </span>
      <span
        className="flex align-items-center gap-2 text-xs white-space-nowrap"
        style={MUTED}
      >
        {dotColor && (
          <span
            className="flex-none border-circle"
            style={{ width: 7, height: 7, background: dotColor }}
          />
        )}
        {label}
      </span>
    </div>
  );
}
