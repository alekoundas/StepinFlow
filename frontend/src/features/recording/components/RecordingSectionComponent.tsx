import type { CSSProperties, ReactNode } from "react";

// The theme's own variables: PrimeFlex 4's surface and text-color classes read tokens this theme
// does not define.
const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

/** One card of the recording's forms, under a small capitalised title. */
export default function RecordingSectionComponent({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section
      className="flex flex-column gap-3 p-4 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        className="m-0 text-sm font-semibold uppercase"
        style={{ ...MUTED, letterSpacing: "0.06em" }}
      >
        {title}
      </h2>
      {children}
    </section>
  );
}
