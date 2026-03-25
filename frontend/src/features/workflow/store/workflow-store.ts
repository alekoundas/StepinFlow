import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";
import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

interface Props {
  selectedTreeNode: TreeNodeDto | undefined;
  selectedFlowStepTypeToAdd: FlowStepTypeEnum | undefined;

  // Actions
  setSelectedTreeNode: (dto: TreeNodeDto | undefined) => void;
  setSelectedFlowStepTypeToAdd: (type: FlowStepTypeEnum | undefined) => void;
}

export const useWorkflowStore = create<Props>()(
  devtools((set, get) => ({
    selectedTreeNode: undefined,
    selectedFlowStepTypeToAdd: undefined,

    setSelectedTreeNode: (dto: TreeNodeDto | undefined): void =>
      set({
        selectedTreeNode: dto,
      }),

    setSelectedFlowStepTypeToAdd: (type: FlowStepTypeEnum | undefined): void =>
      set({
        selectedFlowStepTypeToAdd: type,
      }),
  })),
);
