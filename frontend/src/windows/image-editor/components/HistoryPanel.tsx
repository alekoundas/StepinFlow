/**
 * HistoryPanel — undo/redo stack with thumbnails.
 * Newest entry first; click any row to jump to that state.
 */

import { ScrollPanel } from "primereact/scrollpanel";
import { classNames } from "primereact/utils";
import type { HistoryEntry } from "@/windows/image-editor/types";

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
    <div className="image-editor__history">
      <div className="image-editor__panel-title">
        History
        <span className="image-editor__hint">{entries.length}</span>
      </div>

      <ScrollPanel className="image-editor__history-scroll">
        {entries.length === 0 && (
          <div className="image-editor__empty">No edits yet</div>
        )}

        {entries
          .map((entry, index) => ({ entry, index }))
          .reverse()
          .map(({ entry, index }) => (
            <button
              key={entry.id}
              type="button"
              onClick={() => onSelect(index)}
              className={classNames("image-editor__history-item", {
                "image-editor__history-item--active": index === currentIndex,
                "image-editor__history-item--undone": index > currentIndex,
              })}
            >
              <img src={entry.thumbnail} alt="" draggable={false} />
              <span className="image-editor__history-text">
                <span className="image-editor__history-label">{entry.label}</span>
                <span className="image-editor__history-time">
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </span>
              </span>
            </button>
          ))}
      </ScrollPanel>
    </div>
  );
}
