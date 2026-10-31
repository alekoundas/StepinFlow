import { Button } from "primereact/button";

import { INSET, MUTED } from "@/features/execution/execution-format";

interface Props {
  passes: number;
  firstPass: number;
  lastPass: number;
  stepCount: number;
  isOpen: boolean;

  /** Every row the same, so the list can draw only those in view. */
  height: number;

  onToggle: () => void;
}

/** A run of loop passes that found nothing, as one row. Opening it puts them back under it. */
export default function ExecutionFoldRowComponent({
  passes,
  firstPass,
  lastPass,
  stepCount,
  isOpen,
  height,
  onToggle,
}: Props) {
  return (
    <div
      className="flex align-items-center gap-2 border-round-lg text-sm"
      style={{
        ...INSET,
        height,
        boxSizing: "border-box",
        padding: "0 6px 0 10px",
        whiteSpace: "nowrap",
      }}
    >
      <span style={{ width: 48, flex: "none" }} />

      <i
        className="pi pi-sort"
        style={{ ...MUTED, fontSize: "0.75rem", width: 14, flex: "none" }}
      />

      <span className="min-w-0 overflow-hidden text-overflow-ellipsis">
        {passes.toLocaleString()} passes that found nothing{" "}
        <span style={MUTED}>
          · {firstPass.toLocaleString()}–{lastPass.toLocaleString()} · {stepCount.toLocaleString()} steps
        </span>
      </span>

      <Button
        label={isOpen ? "Fold" : "Show"}
        size="small"
        outlined
        className="ml-auto py-0 px-2 text-xs"
        style={{ height: 22, flex: "none" }}
        onClick={onToggle}
      />
    </div>
  );
}
