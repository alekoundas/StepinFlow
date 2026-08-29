import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  /** Steps the run should park on. Sent once at start, and again whenever they change mid run. */
  breakpointStepIds: number[];
  historyLevel: ExecutionHistoryLevelEnum;

  runState: RunStateEnum;
  executionId: number | undefined;

  /** Filled live from broadcasts while running, and from the database when a past run is opened. */
  executionSteps: ExecutionStepDto[];
  selectedSequence: number | undefined;

  /** The step the run is sat on. Null once nothing is running. */
  currentFlowStepId: number | undefined;
  currentStepName: string | undefined;

  // Actions
  toggleBreakpoint: (flowStepId: number) => void;
  setHistoryLevel: (level: ExecutionHistoryLevelEnum) => void;

  setRunState: (state: RunStateEnum) => void;
  setExecutionId: (id: number | undefined) => void;

  addExecutionStep: (step: ExecutionStepDto) => void;
  setExecutionSteps: (steps: ExecutionStepDto[]) => void;
  setSelectedSequence: (sequence: number | undefined) => void;

  setCurrentStep: (flowStepId: number | undefined, name: string | undefined) => void;

  resetRun: () => void;
}

export const useExecutionStore = create<Props>()(
  devtools((set) => ({
    breakpointStepIds: [],
    historyLevel: ExecutionHistoryLevelEnum.STEPS_AND_IMAGES,

    runState: RunStateEnum.FINISHED,
    executionId: undefined,

    executionSteps: [],
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

    // Appended rather than replaced: a run of five hundred steps must not rebuild the list five
    // hundred times, and a row never changes once it has arrived.
    addExecutionStep: (step: ExecutionStepDto): void =>
      set((state) => ({ executionSteps: [...state.executionSteps, step] })),

    setExecutionSteps: (steps: ExecutionStepDto[]): void =>
      set({ executionSteps: steps }),

    setSelectedSequence: (sequence: number | undefined): void =>
      set({ selectedSequence: sequence }),

    setCurrentStep: (flowStepId: number | undefined, name: string | undefined): void =>
      set({ currentFlowStepId: flowStepId, currentStepName: name }),

    // Breakpoints and the history level survive: they are what you set up, not what happened.
    resetRun: (): void =>
      set({
        executionId: undefined,
        executionSteps: [],
        selectedSequence: undefined,
        currentFlowStepId: undefined,
        currentStepName: undefined,
      }),
  })),
);
