import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type { RecordingDraftPositionDto } from "@/shared/models/recording-draft-position-dto";

interface Props {
  /**
   * Where the steps go in a flow that already existed: the editor's "Record steps here". Undefined
   * when the recording makes a flow of its own.
   */
  target: RecordingDraftPositionDto | undefined;

  /** Whether the flow the recorder is about to create should be a sub-flow. */
  createAsSubFlow: boolean;

  /**
   * The flow the recording setup created. The recording is made against its main area, and a
   * discard deletes it while nothing but the setup is in it. Undefined when recording into a flow
   * that already existed.
   */
  setupFlowId: number | undefined;

  setTarget: (target: RecordingDraftPositionDto | undefined) => void;
  setCreateAsSubFlow: (isSubFlow: boolean) => void;
  setSetupFlowId: (flowId: number | undefined) => void;

  reset: () => void;
}

const initial = {
  target: undefined,
  createAsSubFlow: false,
  setupFlowId: undefined,
};

export const useRecordingStore = create<Props>()(
  devtools((set) => ({
    ...initial,

    setTarget: (target) => set({ target }),
    setCreateAsSubFlow: (isSubFlow) => set({ createAsSubFlow: isSubFlow }),
    setSetupFlowId: (flowId) => set({ setupFlowId: flowId }),

    reset: () => set(initial),
  })),
);
