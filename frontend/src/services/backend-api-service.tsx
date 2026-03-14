import type { RequestMessage } from "../../../electron/shared/types";

// TODO remove this. Buut Build process throws error without it....
// const backendApi = window.backendApi; // old way
declare const backendApi: {
  invoke: <T>(msg: any) => Promise<T>;
  onMessage: <T>(cb: (msg: T) => void) => () => void;
};
///
//

export const backendApiService = {
  greet: (name: string) => invoke<{ greeting: string }>("greet", { name }),

  createFlow: (dto: { name: string; orderNumber?: number }) =>
    invoke<{ newFlowId: number; success: boolean }>("create-flow", dto),

  createFlowStep: (dto: {
    name: string;
    flowStepType: string; // or your enum string
    orderNumber: number;
    flowId: number;
    // ... other fields you send
  }) =>
    invoke<{ newFlowStepId: number; success: boolean }>(
      "create-flow-step",
      dto,
    ),

  // load-flow, save-config, etc.
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
