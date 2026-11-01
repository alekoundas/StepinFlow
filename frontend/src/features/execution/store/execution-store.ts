import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";
import { ExecutionTally, type ExecutionOverview } from "@/features/execution/execution-overview";
import {
  findStep,
  releaseOldestRows,
  releaseQuietPasses,
  type KeptRows,
  type QuietRun,
  type ReleasedRows,
} from "@/features/execution/execution-list";

// The newest rows stay whole, so a pass that just ended opens without asking the history.
const KEEP_NEWEST_ROWS = 2_000;

// Letting go is a walk over every row kept, so it waits until this many more have arrived.
const RELEASE_EVERY_ROWS = 1_000;

// Hours of passes that each found something still have to stop somewhere.
const MAX_KEPT_ROWS = 20_000;

interface Props {
  /** Steps the run should park on. Sent once at start, and again whenever they change mid run. */
  breakpointStepIds: number[];
  historyLevel: ExecutionHistoryLevelEnum;

  runState: RunStateEnum;
  executionId: number | undefined;

  /** How the live execution ended, once it has. */
  endedStatus: ExecutionStatusEnum | undefined;

  /**
   * Filled live from broadcasts while running, and from the database when a past run is opened. A
   * past run keeps every row; a live one keeps its newest and the passes that found something, and
   * counts the rest in quietRuns and released.
   */
  executionSteps: ExecutionStepDto[];
  quietRuns: QuietRun[];
  released: ReleasedRows | undefined;

  /** Over every row that arrived, kept or not. */
  overview: ExecutionOverview;

  /** Folds open in the list, by their first sequence, and the rows read back for those let go of. */
  openFolds: ReadonlySet<number>;
  foldRows: ReadonlyMap<number, ExecutionStepDto[]>;

  selectedSequence: number | undefined;

  /** The step the run is sat on. Null once nothing is running. */
  currentFlowStepId: number | undefined;
  currentStepName: string | undefined;

  // Actions
  toggleBreakpoint: (flowStepId: number) => void;
  setHistoryLevel: (level: ExecutionHistoryLevelEnum) => void;

  setRunState: (state: RunStateEnum) => void;
  setExecutionId: (id: number | undefined) => void;
  setEndedStatus: (status: ExecutionStatusEnum | undefined) => void;

  addExecutionSteps: (steps: ExecutionStepDto[]) => void;
  setExecutionSteps: (steps: ExecutionStepDto[], errorFlowStepId?: number | null) => void;
  setSelectedSequence: (sequence: number | undefined) => void;

  toggleFold: (firstSequence: number) => void;
  addFoldRows: (firstSequence: number, steps: ExecutionStepDto[]) => void;

  setCurrentStep: (flowStepId: number | undefined, name: string | undefined) => void;

  resetRun: () => void;
}

// Not state: nothing draws them, and the tally changes in place.
let tally = new ExecutionTally();
let rowsAtLastRelease = 0;

export const useExecutionStore = create<Props>()(
  devtools((set) => ({
    breakpointStepIds: [],
    historyLevel: ExecutionHistoryLevelEnum.STEPS_AND_IMAGES,

    runState: RunStateEnum.FINISHED,
    executionId: undefined,
    endedStatus: undefined,

    executionSteps: [],
    quietRuns: [],
    released: undefined,
    overview: tally.toOverview(),

    openFolds: new Set(),
    foldRows: new Map(),

    selectedSequence: undefined,

    currentFlowStepId: undefined,
    currentStepName: undefined,

    toggleBreakpoint: (flowStepId: number): void =>
      set((state) => ({
        breakpointStepIds: state.breakpointStepIds.includes(flowStepId)
          ? state.breakpointStepIds.filter((x) => x !== flowStepId)
          : [...state.breakpointStepIds, flowStepId],
      })),

    setHistoryLevel: (level: ExecutionHistoryLevelEnum): void =>
      set({ historyLevel: level }),

    setRunState: (state: RunStateEnum): void => set({ runState: state }),

    setExecutionId: (id: number | undefined): void => set({ executionId: id }),

    setEndedStatus: (status: ExecutionStatusEnum | undefined): void =>
      set({ endedStatus: status }),

    // Appended a batch at a time, and a row never changes once it has arrived, so a memoised row
    // draws once.
    addExecutionSteps: (steps: ExecutionStepDto[]): void =>
      set((state) => {
        tally.add(steps);

        let kept: KeptRows = {
          executionSteps: [...state.executionSteps, ...steps],
          quietRuns: state.quietRuns,
          released: state.released,
        };
        let foldRows = state.foldRows;

        if (kept.executionSteps.length - rowsAtLastRelease >= RELEASE_EVERY_ROWS) {
          const { kept: afterQuiet, rowsByRun } = releaseQuietPasses(kept, KEEP_NEWEST_ROWS);
          kept = releaseOldestRows(afterQuiet, MAX_KEPT_ROWS);
          foldRows = keepOpenFoldRows(foldRows, state.openFolds, rowsByRun, kept.quietRuns);
          rowsAtLastRelease = kept.executionSteps.length;
        }

        return { ...kept, foldRows, overview: tally.toOverview() };
      }),

    setExecutionSteps: (steps: ExecutionStepDto[], errorFlowStepId?: number | null): void => {
      tally = new ExecutionTally();
      tally.add(steps);
      tally.markFatal(steps, errorFlowStepId);
      rowsAtLastRelease = steps.length;

      set({
        executionSteps: steps,
        quietRuns: [],
        released: undefined,
        overview: tally.toOverview(),
        openFolds: new Set(),
        foldRows: new Map(),
      });
    },

    setSelectedSequence: (sequence: number | undefined): void =>
      set({ selectedSequence: sequence }),

    // Folded again, the rows read back for it are let go of too.
    toggleFold: (firstSequence: number): void =>
      set((state) => {
        const openFolds = new Set(state.openFolds);
        const foldRows = new Map(state.foldRows);

        if (openFolds.delete(firstSequence)) foldRows.delete(firstSequence);
        else openFolds.add(firstSequence);

        return { openFolds, foldRows };
      }),

    // Passes let go of while it was being read back are in the fold already, and stay.
    addFoldRows: (firstSequence: number, steps: ExecutionStepDto[]): void =>
      set((state) => {
        if (!state.openFolds.has(firstSequence)) return {};

        const bySequence = new Map<number, ExecutionStepDto>();
        for (const step of state.foldRows.get(firstSequence) ?? []) bySequence.set(step.sequence, step);
        for (const step of steps) bySequence.set(step.sequence, step);

        const foldRows = new Map(state.foldRows);
        foldRows.set(
          firstSequence,
          [...bySequence.values()].sort((a, b) => a.sequence - b.sequence),
        );

        return { foldRows };
      }),

    setCurrentStep: (flowStepId: number | undefined, name: string | undefined): void =>
      set({ currentFlowStepId: flowStepId, currentStepName: name }),

    // Breakpoints and the history level survive: they are what you set up, not what happened.
    resetRun: (): void => {
      tally = new ExecutionTally();
      rowsAtLastRelease = 0;

      set({
        executionId: undefined,
        endedStatus: undefined,
        executionSteps: [],
        quietRuns: [],
        released: undefined,
        overview: tally.toOverview(),
        openFolds: new Set(),
        foldRows: new Map(),
        selectedSequence: undefined,
        currentFlowStepId: undefined,
        currentStepName: undefined,
      });
    },
  })),
);

/** A row kept, or one read back for an open fold. */
export function findKeptStep(
  state: { executionSteps: ExecutionStepDto[]; foldRows: ReadonlyMap<number, ExecutionStepDto[]> },
  sequence: number,
): ExecutionStepDto | undefined {
  const kept = findStep(state.executionSteps, sequence);
  if (kept) return kept;

  for (const rows of state.foldRows.values()) {
    const step = findStep(rows, sequence);
    if (step) return step;
  }

  return undefined;
}

// A fold open while its passes are let go of takes their rows with it, rather than reading them back.
function keepOpenFoldRows(
  foldRows: ReadonlyMap<number, ExecutionStepDto[]>,
  openFolds: ReadonlySet<number>,
  rowsByRun: Map<QuietRun, ExecutionStepDto[]>,
  quietRuns: QuietRun[],
): ReadonlyMap<number, ExecutionStepDto[]> {
  const runFirsts = new Set(quietRuns.map((x) => x.firstSequence));
  const next = new Map([...foldRows].filter(([firstSequence]) => runFirsts.has(firstSequence)));

  for (const [run, rows] of rowsByRun)
    if (openFolds.has(run.firstSequence) && runFirsts.has(run.firstSequence))
      next.set(run.firstSequence, [...(next.get(run.firstSequence) ?? []), ...rows]);

  return next;
}
