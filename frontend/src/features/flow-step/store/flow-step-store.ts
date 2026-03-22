import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { backendApiService } from "@/services/backend-api-service";

import { FlowStepDto } from "@/shared/models/flow-step/flow-step-dto";

interface Props {
  loading: boolean;
  error: string | null;
  version: number; // triggers reloads

  // Actions
  createFlowStep: (dto: FlowStepDto) => Promise<void>;
  updateFlowStep: (id: number, dto: FlowStepDto) => Promise<void>;
  deleteFlowStep: (id: number) => Promise<void>;
  cloneFlowStep: (id: number) => Promise<void>;

  setError: (error: string | null) => void;
  incrementVersion: () => void;
}

export const useFlowStepStore = create<Props>()(
  devtools((set, get) => ({
    loading: false,
    error: null,

    create: async (dto: Partial<FlowStepDto>) => {
      set({ loading: true });
      try {
        await backendApiService.FlowStep.create(new FlowStepDto(dto));
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    update: async (_id: number, _dto: FlowStepDto) => {
      set({ loading: true });
      try {
        //   await backendApiService.Flow.update(id, dto);
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    delete: async (id: number) => {
      set({ loading: true });
      try {
        await backendApiService.Flow.delete(id);
        get().incrementVersion();
        // await get().fetchFlows();
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    clone: async (_id: number) => {
      set({ loading: true });
      try {
        // await backendApiService.Flow.clone(id);
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    setError: (error) => set({ error }),
    incrementVersion: () => set((state) => ({ version: state.version + 1 })),
  })),
);
