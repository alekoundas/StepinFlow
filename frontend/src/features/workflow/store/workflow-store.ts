import { create } from "zustand";
import { devtools } from "zustand/middleware";
import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

/** The editor's tabs: the flow's steps and the parts they use, then the script they make. */
export type WorkflowTab = "flow" | "areas" | "locations" | "inputs" | "templates" | "script";

/** How large the tree is drawn, in percent. The user's own, kept on this machine like the splitter's place. */
export const TREE_ZOOM = { min: 70, max: 130, step: 10, initial: 100 };

const TREE_ZOOM_KEY = "workflow-tree-zoom";

const savedTreeZoom = (): number => {
  const saved = Number(localStorage.getItem(TREE_ZOOM_KEY));
  return saved >= TREE_ZOOM.min && saved <= TREE_ZOOM.max ? saved : TREE_ZOOM.initial;
};

/** The step Copy took. One at a time, kept after a paste, and only for the flow it came from. */
export interface CopiedStep {
  id: number;
  name: string;
  flowStepType?: FlowStepTypeEnum;
}

interface Props {
  selectedTreeNode: TreeNodeDto | undefined;
  selectedFlowStepTypeToAdd: FlowStepTypeEnum | undefined;
  treeRefreshTrigger: {
    id: number;
    isFlow: boolean;
    selectNodeIdAfterLoad?: number;
  } | null;
  rootFlowId: number | undefined;
  copiedStep: CopiedStep | undefined;
  activeTab: WorkflowTab;
  treeZoom: number;

  /** A step to open in the tree, its branches expanded down to it. Set from a "Used by" list. */
  stepToReveal: number | null;

  // Actions
  setSelectedTreeNode: (dto: TreeNodeDto | undefined) => void;
  setSelectedFlowStepTypeToAdd: (type: FlowStepTypeEnum | undefined) => void;

  setTreeRefreshTrigger: (
    trigger: {
      id: number;
      isFlow: boolean;
      selectNodeIdAfterLoad?: number;
    } | null,
  ) => void;

  setRootFlowId: (id: number | undefined) => void;
  setCopiedStep: (step: CopiedStep | undefined) => void;
  setActiveTab: (tab: WorkflowTab) => void;
  setTreeZoom: (percent: number) => void;
  setStepToReveal: (flowStepId: number | null) => void;
}

export const useWorkflowStore = create<Props>()(
  devtools((set) => ({
    selectedTreeNode: undefined,
    selectedFlowStepTypeToAdd: undefined,
    treeRefreshTrigger: undefined,
    rootFlowId: undefined,
    copiedStep: undefined,
    activeTab: "flow",
    treeZoom: savedTreeZoom(),
    stepToReveal: null,

    setSelectedTreeNode: (dto: TreeNodeDto | undefined): void =>
      set({
        selectedTreeNode: dto,
      }),

    setSelectedFlowStepTypeToAdd: (type: FlowStepTypeEnum | undefined): void =>
      set({
        selectedFlowStepTypeToAdd: type,
      }),

    setTreeRefreshTrigger: (trigger): void =>
      set({ treeRefreshTrigger: trigger }),

    setRootFlowId: (id: number | undefined): void => set({ rootFlowId: id }),

    setCopiedStep: (step: CopiedStep | undefined): void => set({ copiedStep: step }),

    setActiveTab: (tab: WorkflowTab): void => set({ activeTab: tab }),

    setTreeZoom: (percent: number): void => {
      const treeZoom = Math.min(TREE_ZOOM.max, Math.max(TREE_ZOOM.min, percent));
      localStorage.setItem(TREE_ZOOM_KEY, String(treeZoom));
      set({ treeZoom });
    },

    // Opening a step is opening the Flow tab too.
    setStepToReveal: (flowStepId: number | null): void =>
      set(flowStepId == null ? { stepToReveal: null } : { stepToReveal: flowStepId, activeTab: "flow" }),
  })),
);
