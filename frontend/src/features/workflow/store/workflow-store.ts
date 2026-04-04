import { create } from "zustand";
import { devtools } from "zustand/middleware";
import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  selectedTreeNode: TreeNodeDto | undefined;
  selectedFlowStepTypeToAdd: FlowStepTypeEnum | undefined;
  treeRefreshTrigger: {
    id: number;
    isFlow: boolean;
    selectNodeIdAfterLoad?: number;
  } | null;
  rootFlowId: number | undefined;

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
}

export const useWorkflowStore = create<Props>()(
  devtools((set, _get) => ({
    selectedTreeNode: undefined,
    selectedFlowStepTypeToAdd: undefined,
    treeRefreshTrigger: undefined,
    rootFlowId: undefined,

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
  })),
);
