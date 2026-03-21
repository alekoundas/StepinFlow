import type { FlowDto } from "@/shared/models/flow/flow-dto";
import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { backendApiService } from "@/services/backend-api-service";
import { FlowCreateDto } from "@/shared/models/flow/flow-create-dto";

interface IFlowState {
  loading: boolean;
  error: string | null;
  version: number; // triggers reloads

  // Actions
  createFlow: (dto: FlowCreateDto) => Promise<void>;
  updateFlow: (id: number, dto: FlowDto) => Promise<void>;
  deleteFlow: (id: number) => Promise<void>;
  cloneFlow: (id: number) => Promise<void>;

  setError: (error: string | null) => void;
  incrementVersion: () => void;
}

export const useFlowStore = create<IFlowState>()(
  devtools((set, get) => ({
    loading: false,
    error: null,


    createFlow: async (dto: Partial<FlowCreateDto>) => {
      set({ loading: true });
      try {
        await backendApiService.Flow.create(new FlowCreateDto(dto));
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    updateFlow: async (_id, _dto) => {
      set({ loading: true });
      try {
        //   await backendApiService.Flow.update(id, dto);
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    deleteFlow: async (id) => {
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

    cloneFlow: async (_id) => {
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
