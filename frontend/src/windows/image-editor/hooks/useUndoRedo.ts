/**
 * useUndoRedo — history stack for the image editor.
 *
 * Every entry keeps a full pixel snapshot (a detached canvas) plus a small
 * thumbnail for the history list. Snapshots of a full virtual-desktop
 * screenshot are large (width * height * 4 bytes), so the stack evicts the
 * oldest entries once it goes over `memoryBudgetBytes`.
 */

import { useCallback, useMemo, useState } from "react";
import type { HistoryEntry } from "@/windows/image-editor/types";
import {
  canvasByteSize,
  canvasToThumbnail,
  cloneCanvas,
} from "@/windows/image-editor/utils/canvas-utils";

const DEFAULT_BUDGET_BYTES = 256 * 1024 * 1024;
const MAX_ENTRIES = 40;

interface HistoryState {
  entries: HistoryEntry[];
  index: number;
}

export function useUndoRedo(memoryBudgetBytes = DEFAULT_BUDGET_BYTES) {
  const [state, setState] = useState<HistoryState>({ entries: [], index: -1 });

  const createEntry = useCallback(
    (source: HTMLCanvasElement, label: string): HistoryEntry => ({
      id: crypto.randomUUID(),
      label,
      canvas: cloneCanvas(source),
      thumbnail: canvasToThumbnail(source),
      timestamp: Date.now(),
    }),
    [],
  );

  /** Trim from the front until the stack fits the budget. */
  const evict = useCallback(
    (entries: HistoryEntry[]): HistoryEntry[] => {
      let kept = entries;
      let bytes = kept.reduce((sum, e) => sum + canvasByteSize(e.canvas), 0);

      let dropCount = 0;
      while (
        kept.length - dropCount > 1 &&
        (bytes > memoryBudgetBytes || kept.length - dropCount > MAX_ENTRIES)
      ) {
        bytes -= canvasByteSize(kept[dropCount].canvas);
        dropCount++;
      }

      if (dropCount > 0) kept = kept.slice(dropCount);
      return kept;
    },
    [memoryBudgetBytes],
  );

  /** Drop everything and start a new stack from `source`. */
  const reset = useCallback(
    (source: HTMLCanvasElement, label = "Original") => {
      setState({ entries: [createEntry(source, label)], index: 0 });
    },
    [createEntry],
  );

  /** Record a new state. Anything after the current index is discarded. */
  const push = useCallback(
    (source: HTMLCanvasElement, label: string) => {
      const entry = createEntry(source, label);

      setState((prev) => {
        const kept = evict([...prev.entries.slice(0, prev.index + 1), entry]);
        return { entries: kept, index: kept.length - 1 };
      });
    },
    [createEntry, evict],
  );

  /**
   * Move to `index` and hand the caller the snapshot to restore.
   * Returns null when the index is out of range.
   */
  const jumpTo = useCallback(
    (index: number): HistoryEntry | null => {
      const entry = state.entries[index];
      if (!entry) return null;

      setState((prev) => ({ ...prev, index }));
      return entry;
    },
    [state.entries],
  );

  const canUndo = state.index > 0;
  const canRedo = state.index >= 0 && state.index < state.entries.length - 1;

  const undo = useCallback(
    () => (canUndo ? jumpTo(state.index - 1) : null),
    [canUndo, jumpTo, state.index],
  );

  const redo = useCallback(
    () => (canRedo ? jumpTo(state.index + 1) : null),
    [canRedo, jumpTo, state.index],
  );

  return useMemo(
    () => ({
      entries: state.entries,
      index: state.index,
      canUndo,
      canRedo,
      reset,
      push,
      jumpTo,
      undo,
      redo,
    }),
    [state.entries, state.index, canUndo, canRedo, reset, push, jumpTo, undo, redo],
  );
}
