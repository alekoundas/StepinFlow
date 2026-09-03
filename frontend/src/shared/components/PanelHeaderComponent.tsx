import type { ReactNode } from "react";

import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  title: string;

  /** Whatever the panel can do to itself - filters, a jump, an action. */
  children?: ReactNode;
}

/**
 * The strip that names a panel.
 *
 * Three panels side by side are three unlabelled lists otherwise, and which is which has to be
 * worked out from what happens to be in them.
 */
export default function PanelHeaderComponent({ title, children }: Props) {
  return (
    <div
      className="flex align-items-center justify-content-between gap-2 px-3 border-bottom-1 surface-border"
      style={{ paddingTop: "0.6rem", paddingBottom: "0.6rem", minHeight: "2.75rem" }}
    >
      <LabelComponent
        text={title.toUpperCase()}
        size="xs"
        weight="bold"
        color="secondary"
      />

      <div className="flex align-items-center gap-2">{children}</div>
    </div>
  );
}
