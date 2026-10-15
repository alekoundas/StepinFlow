import { create } from "zustand";
import { devtools } from "zustand/middleware";

/** Where a recording's steps go: child number targetIndex of a flow, or of a step. */
export interface RecordingTarget {
  targetFlowId?: number | null;
  targetParentFlowStepId?: number | null;
  targetIndex: number;
}

interface Props {
  /** Where the steps land. */
  target: RecordingTarget | undefined;

  /** Whether the flow the recorder is about to create should be a sub-flow. */
  createAsSubFlow: boolean;

  /**
   * The flow the recording setup created. The recording is made against its main area, and a
   * discard deletes it while nothing but the setup is in it. Undefined when recording into a flow
   * that already existed.
   */
  setupFlowId: number | undefined;

  setTarget: (target: RecordingTarget | undefined) => void;
  setCreateAsSubFlow: (isSubFlow: boolean) => void;
  setSetupFlowId: (flowId: number | undefined) => void;

  reset: () => void;
}

const initial = {
  target: undefined,
  createAsSubFlow: false,
  setupFlowId: undefined,
};

export const useWizardStore = create<Props>()(
  devtools((set) => ({
    ...initial,

    setTarget: (target) => set({ target }),
    setCreateAsSubFlow: (isSubFlow) => set({ createAsSubFlow: isSubFlow }),
    setSetupFlowId: (flowId) => set({ setupFlowId: flowId }),

    reset: () => set(initial),
  })),
);
