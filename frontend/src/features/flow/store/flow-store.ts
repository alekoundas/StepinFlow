import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { Flow } from "../../../models/dto/flow";
import { backendApiService } from "../../../services/backend-api-service";

interface IFlowState {
  flows: Flow[];
  loading: boolean;
  error: string | null;

  // Actions
  fetchFlows: () => Promise<void>;
  createFlow: (dto: Flow) => Promise<void>;
  updateFlow: (id: number, dto: Flow) => Promise<void>;
  deleteFlow: (id: number) => Promise<void>;
  cloneFlow: (id: number) => Promise<void>;

  setError: (error: string | null) => void;
}

export const useFlowStore = create<IFlowState>()(
  devtools((set, get) => ({
    flows: [],
    loading: false,
    error: null,

    fetchFlows: async () => {
      set({ loading: true, error: null });
      try {
        const flows = await backendApiService.Flow.getAll();
        set({ flows, loading: false });
      } catch (err: any) {
        set({ error: err.message, loading: false });
      }
    },

    createFlow: async (dto) => {
      set({ loading: true });
      try {
        await backendApiService.Flow.create(dto);
        await get().fetchFlows(); // refresh list
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
        await get().fetchFlows();
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    deleteFlow: async (_id) => {
      set({ loading: true });
      try {
        // await backendApiService.Flow.delete(id);
        await get().fetchFlows();
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
        await get().fetchFlows();
      } catch (err: any) {
        set({ error: err.message });
      } finally {
        set({ loading: false });
      }
    },

    setError: (error) => set({ error }),
  })),
);
