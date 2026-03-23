import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";

interface Props {
  selectedTreeNode: TreeNodeDto | undefined;

  // Actions
  setSelectedTreeNode: (dto: Partial<TreeNodeDto>) => void;
}

export const useWorkflowStore = create<Props>()(
  devtools((set, get) => ({
    selectedTreeNode: undefined,

    setSelectedTreeNode: (dto: Partial<TreeNodeDto>): void =>
      set({
        selectedTreeNode: {
          ...new TreeNodeDto(),
          ...dto,
        },
      }),
  })),
);
