import type { ReactNode } from "react";

import { CARD_BACKGROUND, iconTile } from "@/features/home/home-format";

interface Props {
  icon: string;
  title: string;

  /** Beside the title: a status, a count, a menu. */
  action?: ReactNode;
  children: ReactNode;
}

/** What every card under the hero stands on. */
export default function HomeCardComponent({ icon, title, action, children }: Props) {
  return (
    <section
      aria-label={title}
      className="flex flex-column gap-3 p-4 border-1 border-round-2xl fadein animation-duration-300"
      style={{ background: CARD_BACKGROUND, borderColor: "var(--surface-border)" }}
    >
      <header
        className="flex align-items-center justify-content-between gap-3"
        style={{ minHeight: 32 }}
      >
        <div className="flex align-items-center gap-2 min-w-0">
          <span
            className="flex align-items-center justify-content-center flex-none border-round-lg"
            style={{ width: 30, height: 30, ...iconTile("var(--primary-color)") }}
          >
            <i
              className={`pi pi-${icon} text-sm`}
              aria-hidden="true"
            />
          </span>
          <h2 className="m-0 text-base font-semibold white-space-nowrap">{title}</h2>
        </div>

        {action}
      </header>

      {children}
    </section>
  );
}
