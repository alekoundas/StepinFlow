import type { ReactNode } from "react";

import { CARD } from "@/features/execution/execution-format";

interface Props {
  title: ReactNode;

  /** Beside the title: a state, a count, a way back. */
  actions?: ReactNode;

  /** A row under the title, for what narrows the panel. */
  toolbar?: ReactNode;

  children: ReactNode;
}

/** One of the three panels: its own surface, its own header, its own scroll. */
export default function ExecutionPanelComponent({ title, actions, toolbar, children }: Props) {
  return (
    <section
      className="flex flex-column h-full border-1 border-round-xl"
      style={{ ...CARD, overflow: "hidden", minHeight: 0 }}
    >
      <header
        className="flex flex-column gap-2 px-3 py-2 border-bottom-1"
        style={{ borderColor: "var(--surface-border)" }}
      >
        <div
          className="flex align-items-center justify-content-between gap-2"
          style={{ minHeight: 30 }}
        >
          <h2 className="m-0 text-sm font-semibold white-space-nowrap">{title}</h2>
          {actions ? <div className="flex align-items-center gap-2">{actions}</div> : null}
        </div>

        {toolbar}
      </header>

      <div
        className="flex-1 flex flex-column"
        style={{ overflow: "auto", minHeight: 0 }}
      >
        {children}
      </div>
    </section>
  );
}
