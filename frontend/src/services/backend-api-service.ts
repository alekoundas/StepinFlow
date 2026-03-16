import type { RequestMessage } from "../../../electron/shared/types";
import { Flow } from "../models/dto/flow";
import { FlowSearchArea } from "../models/dto/flow-search-area";
import { FlowStep } from "../models/dto/flow-step";
import { FlowStepImage } from "../models/dto/flow-step-image";
import { SubFlow } from "../models/dto/sub-flow";

// TODO remove this. Buut Build process throws error without it....
// const backendApi = window.backendApi; // old way
declare const backendApi: {
  invoke: <T>(msg: any) => Promise<T>;
  onMessage: <T>(cb: (msg: T) => void) => () => void;
};
///
//
// const x: string = 123; // ← this should instantly show error
export const backendApiService = {
  greet: (name: string) => invoke<{ greeting: string }>("greet", { name }),

  Flow: {
    create: (dto: Flow) =>
      invoke<{ newId: number; success: boolean }>("Flow.create", dto),
    get: (id: number) => invoke<Flow>("Flow.get", id),
    getAll: () => invoke<Flow[]>("Flow.getAll"),
  },

  FlowStep: {
    create: (dto: FlowStep) =>
      invoke<{ newId: number; success: boolean }>("FlowStep.create", dto),
    get: (id: number) => invoke<FlowStep>("FlowStep.get", id),
  },

  FlowStepImage: {
    create: (dto: FlowStepImage) =>
      invoke<{ newId: number; success: boolean }>("FlowStepImage.create", dto),
    get: (id: number) => invoke<FlowStepImage>("FlowStepImage.get", id),
  },

  FlowSearchArea: {
    create: (dto: FlowSearchArea) =>
      invoke<{ newId: number; success: boolean }>("FlowSearchArea.create", dto),
    get: (id: number) => invoke<FlowSearchArea>("FlowSearchArea.get", id),
  },

  SubFlow: {
    create: (dto: SubFlow) =>
      invoke<{ newId: number; success: boolean }>("SubFlow.create", dto),
    get: (id: number) => invoke<SubFlow>("SubFlow.get", id),
  },
};

async function invoke<T = any>(action: string, payload: any = {}): Promise<T> {
  const msg: RequestMessage = {
    action,
    payload,
  };

  try {
    return await backendApi.invoke<T>(msg);
  } catch (err: any) {
    console.error(`Backend invoke failed [${action}]:`, err);
    throw err;
  }
}

// Optional: Keep this ONLY for unsolicited messages (progress, events, etc.)
export function setupPushListener(callback: (msg: any) => void): () => void {
  return backendApi.onMessage(callback);
}

// Optional: zustand integration example
// import { create } from "zustand";

// type BackendStore = {
//   logs: string[];
//   status: string;
//   addLog: (msg: string) => void;
//   setStatus: (s: string) => void;
// };

// export const useBackendStore = create<BackendStore>((set) => ({
//   logs: [],
//   status: "Ready",
//   addLog: (line) => set((s) => ({ logs: [...s.logs, line] })),
//   setStatus: (s) => set({ status: s }),
// }));

// export function useBackendEvents() {
//   const { addLog } = useBackendStore();

//   useBackendListener((msg: any) => {
//     console.log("Backend message:", msg);

//     if (msg?.action === "d") {
//       // your .NET response action
//       addLog(JSON.stringify(msg.payload));
//     } else if (msg?.event === "progress") {
//       addLog(`Progress: ${msg.details}`);
//     } else {
//       addLog(JSON.stringify(msg));
//     }
//   }, []);
// }
