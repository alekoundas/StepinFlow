import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type {
  DraftStepDto,
  FlowDraftDto,
  FlowDraftTargetDto,
} from "@/shared/models/database/flow-draft-dto";

interface Props {
  /** Where the steps will land, chosen before the recording starts. */
  target: FlowDraftTargetDto | undefined;

  /** What the recorder produced, resolved in place by the wizard. */
  draft: FlowDraftDto | undefined;

  setTarget: (target: FlowDraftTargetDto | undefined) => void;
  setDraft: (draft: FlowDraftDto | undefined) => void;

  /** Replaces one step, matched on tempId, leaving the rest untouched. */
  updateStep: (tempId: number, update: (step: DraftStepDto) => DraftStepDto) => void;
  removeStep: (tempId: number) => void;

  reset: () => void;
}

/**
 * Holds the draft across the hop from the recording page to the wizard. Deliberately not
 * persisted: a half resolved wizard is not worth restoring, and the screenshots it points at
 * live in the backend session anyway.
 */
export const useWizardStore = create<Props>()(
  devtools((set) => ({
    target: undefined,
    draft: undefined,

    setTarget: (target) => set({ target }),
    setDraft: (draft) => set({ draft }),

    updateStep: (tempId, update) =>
      set((state) =>
        state.draft
          ? {
              draft: {
                ...state.draft,
                steps: state.draft.steps.map((step) =>
                  step.tempId === tempId ? update(step) : step,
                ),
              },
            }
          : state,
      ),

    removeStep: (tempId) =>
      set((state) =>
        state.draft
          ? {
              draft: {
                ...state.draft,
                steps: state.draft.steps.filter((step) => step.tempId !== tempId),
              },
            }
          : state,
      ),

    reset: () => set({ target: undefined, draft: undefined }),
  })),
);
