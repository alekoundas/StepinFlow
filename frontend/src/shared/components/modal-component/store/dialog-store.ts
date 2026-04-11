import { DialogDeleteComponent } from "@/shared/components/modal-component/dialogs/DialogDeleteComponent";
import {
  DialogFormComponent,
  type IDialogFormComponentProps,
} from "@/shared/components/modal-component/dialogs/DialogFormComponent";
import { DialogWarningComponent } from "@/shared/components/modal-component/dialogs/DialogWarningComponent";
import type { ReactNode } from "react";
import React from "react";
import { create } from "zustand/react";

type DialogType = "form" | "warning" | "delete" | "custom";

interface Dialog {
  id: string;
  type: DialogType;
  component: ReactNode;
  // props?: Record<string, any>;
}

interface DialogStore {
  dialogs: Dialog[];

  // Typed openers
  openForm: (id: string, props: IDialogFormComponentProps) => void;
  openWarning: (id: string, props: any) => void;
  openDelete: (id: string, props: any) => void;
  openCustom: (id: string, component: ReactNode) => void;

  close: (id: string) => void;
  closeAll: () => void;
}

export const useDialogStore = create<DialogStore>((set) => ({
  dialogs: [],

  openForm: (id, props) =>
    set((state) => ({
      dialogs: [
        ...state.dialogs.filter((d) => d.id !== id),
        {
          id,
          type: "form",
          component: React.createElement(DialogFormComponent, props),
        },
      ],
    })),

  openWarning: (id, props) =>
    set((state) => ({
      dialogs: [
        ...state.dialogs.filter((d) => d.id !== id),
        {
          id,
          type: "warning",
          component: React.createElement(DialogWarningComponent, props),
        },
      ],
    })),

  openDelete: (id, props) =>
    set((state) => ({
      dialogs: [
        ...state.dialogs.filter((d) => d.id !== id),
        {
          id,
          type: "delete",
          component: React.createElement(DialogDeleteComponent, props),
        },
      ],
    })),

  openCustom: (id, component) =>
    set((state) => ({
      dialogs: [
        ...state.dialogs.filter((d) => d.id !== id),
        { id, type: "custom", component },
      ],
    })),

  close: (id) =>
    set((state) => ({
      dialogs: state.dialogs.filter((d) => d.id !== id),
    })),

  closeAll: () => set({ dialogs: [] }),
}));
