/**
 * HistoryPanel Component - Right sidebar showing undo/redo history
 *
 * Features:
 *  - Shows thumbnail previews of each history entry
 *  - Click to jump to any point in history
 *  - Shows action description
 *  - Highlights current state
 */

import { useUndoRedo } from "../hooks/useUndoRedo";

interface HistoryPanelProps {
  history: ReturnType<typeof useUndoRedo>;
  onSelectHistoryItem: (index: number) => void;
}

export default function HistoryPanel({
  history,
  onSelectHistoryItem,
}: HistoryPanelProps) {
  const entries = history.getHistory();

  return (
    <div className="editor-history-panel">
      <h3 className="history-title">History</h3>

      <div className="history-list">
        {entries.length === 0 ? (
          <div className="history-empty">No history yet</div>
        ) : (
          entries.map((entry, index) => (
            <div
              key={index}
              className={`history-item ${
                index === history.currentIndex ? "active" : ""
              }`}
              onClick={() => onSelectHistoryItem(index)}
              title={entry.action}
            >
              {/* Thumbnail */}
              <div className="history-thumbnail">
                {entry.thumbnail ? (
                  <img
                    src={entry.thumbnail}
                    alt={entry.action}
                  />
                ) : (
                  <div className="thumbnail-placeholder">No preview</div>
                )}
              </div>

              {/* Info */}
              <div className="history-info">
                <div className="history-action">{entry.action}</div>
                <div className="history-time">
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </div>
              </div>

              {/* Active indicator */}
              {index === history.currentIndex && (
                <div className="history-active-indicator">✓</div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}
