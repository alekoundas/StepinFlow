import { create } from "zustand";
import { devtools } from "zustand/middleware";
import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

/** The editor's tabs: the flow's steps and the parts they use, then the script they make. */
export type WorkflowTab = "flow" | "areas" | "locations" | "inputs" | "templates" | "script";

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

    // Opening a step is opening the Flow tab too.
    setStepToReveal: (flowStepId: number | null): void =>
      set(flowStepId == null ? { stepToReveal: null } : { stepToReveal: flowStepId, activeTab: "flow" }),
  })),
);
