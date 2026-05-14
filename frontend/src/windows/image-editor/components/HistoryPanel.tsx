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
    <div
      style={{
        width: "200px",
        height: "100%",
        backgroundColor: "#1a1a1a",
        borderLeft: "1px solid #444",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      <h3
        style={{
          margin: "12px",
          fontSize: "14px",
          fontWeight: "600",
          color: "#aaa",
          textTransform: "uppercase",
          letterSpacing: "0.5px",
        }}
      >
        History
      </h3>

      <div
        style={{
          flex: 1,
          overflowY: "auto",
          display: "flex",
          flexDirection: "column",
          gap: "4px",
          padding: "4px",
        }}
      >
        {entries.length === 0 ? (
          <div
            style={{
              padding: "16px",
              textAlign: "center",
              color: "#666",
              fontSize: "12px",
            }}
          >
            No history yet
          </div>
        ) : (
          entries.map((entry, index) => (
            <div
              key={index}
              style={{
                display: "flex",
                gap: "8px",
                padding: "8px",
                backgroundColor:
                  index === history.currentIndex ? "#2a4a7a" : "#222",
                border: `1px solid ${
                  index === history.currentIndex ? "#669bff" : "#333"
                }`,
                borderRadius: "4px",
                cursor: "pointer",
                transition: "all 0.2s ease",
              }}
              onClick={() => onSelectHistoryItem(index)}
              title={entry.action}
            >
              {/* Thumbnail */}
              <div
                style={{
                  width: "50px",
                  height: "50px",
                  backgroundColor: "#111",
                  borderRadius: "3px",
                  overflow: "hidden",
                  flexShrink: 0,
                  border: "1px solid #333",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {entry.thumbnail ? (
                  <img
                    src={entry.thumbnail}
                    alt={entry.action}
                    style={{ width: "100%", height: "100%", objectFit: "contain" }}
                  />
                ) : (
                  <div style={{ fontSize: "10px", color: "#555" }}>
                    No preview
                  </div>
                )}
              </div>

              {/* Info */}
              <div style={{ flex: 1, minWidth: 0 }}>
                <div
                  style={{
                    fontSize: "12px",
                    fontWeight: "500",
                    color: "#fff",
                    whiteSpace: "nowrap",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                  }}
                >
                  {entry.action}
                </div>
                <div
                  style={{
                    fontSize: "10px",
                    color: "#888",
                    marginTop: "2px",
                  }}
                >
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </div>
              </div>

              {/* Active indicator */}
              {index === history.currentIndex && (
                <div
                  style={{
                    width: "20px",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    color: "#28a745",
                    fontWeight: "bold",
                    flexShrink: 0,
                  }}
                >
                  ✓
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}
