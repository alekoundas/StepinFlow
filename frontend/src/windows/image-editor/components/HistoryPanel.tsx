/**
 * HistoryPanel — undo/redo stack with thumbnails.
 * Newest entry first; click any row to jump to that state.
 */

import { ScrollPanel } from "primereact/scrollpanel";
import { classNames } from "primereact/utils";
import type { HistoryEntry } from "@/windows/image-editor/types";

const THUMBNAIL_STYLE: React.CSSProperties = {
  width: "3.5rem",
  height: "2.25rem",
  objectFit: "contain",
  background: "#1b1e25",
};

interface HistoryPanelProps {
  entries: HistoryEntry[];
  currentIndex: number;
  onSelect: (index: number) => void;
}

export default function HistoryPanel({
  entries,
  currentIndex,
  onSelect,
}: HistoryPanelProps) {
  return (
    <div className="flex flex-column flex-1 min-h-0 p-3 border-top-1 surface-border">
      <div className="flex align-items-center justify-content-between mb-2 text-xs font-semibold uppercase text-color-secondary">
        History
        <span>{entries.length}</span>
      </div>

      {/* A floor so the list cant be squeezed away by a tall options panel. */}
      <ScrollPanel className="flex-1" style={{ minHeight: "6rem" }}>
        {entries.length === 0 && (
          <div className="p-2 text-sm text-color-secondary">No edits yet</div>
        )}

        {entries
          .map((entry, index) => ({ entry, index }))
          .reverse()
          .map(({ entry, index }) => (
            <button
              key={entry.id}
              type="button"
              onClick={() => onSelect(index)}
              className={classNames(
                "flex align-items-center gap-2 w-full p-1 mb-1 text-left cursor-pointer border-1 border-round",
                index === currentIndex
                  ? "border-primary"
                  : "border-transparent bg-transparent hover:surface-hover",
                index > currentIndex && "opacity-40",
              )}
              style={{
                // Inherit rather than take the browser button defaults, and
                // leave background to the classes unless this row is current
                // (an inline background would beat the hover class).
                color: "inherit",
                font: "inherit",
                background:
                  index === currentIndex
                    ? "var(--highlight-bg, rgba(56, 189, 248, 0.15))"
                    : undefined,
              }}
            >
              <img
                src={entry.thumbnail}
                alt=""
                draggable={false}
                className="border-1 surface-border border-round-xs"
                style={THUMBNAIL_STYLE}
              />
              <span className="flex flex-column min-w-0">
                <span className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis">
                  {entry.label}
                </span>
                <span className="text-xs text-color-secondary">
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </span>
              </span>
            </button>
          ))}
      </ScrollPanel>
    </div>
  );
}
