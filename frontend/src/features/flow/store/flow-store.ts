import { create } from "zustand";
import { devtools } from "zustand/middleware";

interface Props {
  // loading: boolean;
  // error: string | null;
  // version: number; // triggers reloads
  // Actions
  // createFlow: (dto: FlowCreateDto) => Promise<void>;
  // updateFlow: (id: number, dto: FlowDto) => Promise<void>;
  // deleteFlow: (id: number) => Promise<void>;
  // cloneFlow: (id: number) => Promise<void>;
  // setError: (error: string | null) => void;
  // incrementVersion: () => void;
}

export const useFlowStore = create<Props>()(
  devtools((_set, _get) => ({
    // loading: false,
    // error: null,
    // createFlow: async (dto: Partial<FlowCreateDto>) => {
    //   set({ loading: true });
    //   try {
    //     await backendApiService.Flow.create(new FlowDto(dto));
    //   } catch (err: any) {
    //     set({ error: err.message });
    //   } finally {
    //     set({ loading: false });
    //   }
    // },
    // updateFlow: async (_id: number, _dto: FlowDto) => {
    //   set({ loading: true });
    //   try {
    //     //   await backendApiService.Flow.update(id, dto);
    //   } catch (err: any) {
    //     set({ error: err.message });
    //   } finally {
    //     set({ loading: false });
    //   }
    // },
    // deleteFlow: async (id: number) => {
    //   set({ loading: true });
    //   try {
    //     await backendApiService.Flow.delete(id);
    //     get().incrementVersion();
    //     // await get().fetchFlows();
    //   } catch (err: any) {
    //     set({ error: err.message });
    //   } finally {
    //     set({ loading: false });
    //   }
    // },
    // cloneFlow: async (_id: number) => {
    //   set({ loading: true });
    //   try {
    //     // await backendApiService.Flow.clone(id);
    //   } catch (err: any) {
    //     set({ error: err.message });
    //   } finally {
    //     set({ loading: false });
    //   }
    // },
    // setError: (error) => set({ error }),
    // incrementVersion: () => set((state) => ({ version: state.version + 1 })),
  })),
);
