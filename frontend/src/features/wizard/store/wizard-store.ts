import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type {
  DraftStepDto,
  FlowDraftTargetDto,
} from "@/shared/models/database/flow-draft-dto";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";

interface Props {
  /** Where the steps land. */
  target: FlowDraftTargetDto | undefined;

  /**
   * The flow the step forms look areas and points up against. Not the same as target.flowId:
   * when the steps land under a parent step the target names the step, not the flow.
   */
  lookupFlowId: number | undefined;

  /** What the recorder captured, in order. Never mutated once set. */
  actions: RecordedActionDto[];

  /** How far through the actions the user has decided. */
  cursor: number;

  /** Steps confirmed so far, in save order. */
  steps: DraftStepDto[];

  /**
   * The branch new steps are currently landing in, or undefined at the top level. Set when the
   * user places an action inside a previous step Success branch.
   */
  openParentTempId: number | undefined;
  openParentBranch: FlowStepTypeEnum | undefined;

  setTarget: (target: FlowDraftTargetDto | undefined, lookupFlowId?: number) => void;
  setActions: (actions: RecordedActionDto[]) => void;

  addSteps: (
    steps: DraftStepDto[],
    open: { parentTempId?: number; parentBranch?: FlowStepTypeEnum },
  ) => void;

  updateStep: (tempId: number, update: (step: DraftStepDto) => DraftStepDto) => void;

  /**
   * Rewinds to an action, dropping every decision made after it.
   *
   * Placement cascades, so a later step can be a child of the one being changed. Keeping those
   * would leave steps parented to a shape that no longer exists, which is worse than losing
   * work the user is already choosing to redo.
   */
  rewindTo: (cursor: number) => void;

  reset: () => void;
}

const initial = {
  target: undefined,
  lookupFlowId: undefined,
  actions: [],
  cursor: 0,
  steps: [],
  openParentTempId: undefined,
  openParentBranch: undefined,
};

export const useWizardStore = create<Props>()(
  devtools((set) => ({
    ...initial,

    setTarget: (target, lookupFlowId) =>
      set({ target, lookupFlowId: lookupFlowId ?? target?.targetFlowId ?? undefined }),
    setActions: (actions) =>
      set({ actions, cursor: 0, steps: [], openParentTempId: undefined, openParentBranch: undefined }),

    addSteps: (steps, open) =>
      set((state) => ({
        steps: [...state.steps, ...steps],
        cursor: state.cursor + 1,
        openParentTempId: open.parentTempId,
        openParentBranch: open.parentBranch,
      })),

    updateStep: (tempId, update) =>
      set((state) => ({
        steps: state.steps.map((step) => (step.tempId === tempId ? update(step) : step)),
      })),

    rewindTo: (cursor) =>
      set((state) => {
        // Every step carries the index of the action that produced it, so truncating the steps
        // is the same question as truncating the actions.
        const kept = state.steps.filter((step) => (step.actionIndex ?? 0) < cursor);
        const last = kept[kept.length - 1];

        return {
          cursor,
          steps: kept,
          openParentTempId: last?.parentTempId ?? undefined,
          openParentBranch: last?.parentBranch ?? undefined,
        };
      }),

    reset: () => set(initial),
  })),
);
