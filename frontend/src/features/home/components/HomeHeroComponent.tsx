import type { CSSProperties, ReactNode } from "react";

import { CARD_BACKGROUND, MUTED } from "@/features/home/home-format";

const HERO: CSSProperties = {
  background: `linear-gradient(115deg, color-mix(in srgb, var(--primary-color) 24%, var(--surface-card)) 0%, ${CARD_BACKGROUND} 65%)`,
  borderColor: "color-mix(in srgb, var(--primary-color) 30%, var(--surface-border))",
};

interface Props {
  eyebrow: ReactNode;
  title: ReactNode;
  description?: string;

  /** The buttons, on the right. */
  children?: ReactNode;
}

/** The one thing Home leads with: what to do next. */
export default function HomeHeroComponent({ eyebrow, title, description, children }: Props) {
  return (
    <section
      className="flex align-items-center justify-content-between flex-wrap gap-4 p-4 border-1 border-round-2xl fadein animation-duration-300"
      style={HERO}
    >
      <div
        className="flex flex-column gap-2 min-w-0"
        style={{ flex: "1 1 280px" }}
      >
        <div
          className="flex align-items-center gap-2 text-sm"
          style={MUTED}
        >
          {eyebrow}
        </div>
        <h2
          className="m-0 text-2xl font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis"
          style={{ letterSpacing: "-0.02em" }}
        >
          {title}
        </h2>
        {description && (
          <p
            className="m-0 text-sm"
            style={MUTED}
          >
            {description}
          </p>
        )}
      </div>

      <div className="flex align-items-center flex-wrap gap-2">{children}</div>
    </section>
  );
}
