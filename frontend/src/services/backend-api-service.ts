import type { RequestMessage } from "../../../electron/shared/types";
import type { DataTableDto } from "@/shared/models/data-table/datatable-dto";
import type { DataTableResponseDto } from "@/shared/models/data-table/datatable-response-dto";
import type { FlowSearchAreaCreateDto } from "@/shared/models/flow-search-area/flow-search-area-create-dto";
import type { FlowSearchAreaDto } from "@/shared/models/flow-search-area/flow-search-area-dto";
import type { FlowStepImageCreateDto } from "@/shared/models/flow-step-image/flow-step-image-create-dto";
import type { FlowStepImageDto } from "@/shared/models/flow-step-image/flow-step-image-dto";
import type { FlowStepCreateDto } from "@/shared/models/flow-step/flow-step-create-dto";
import type { FlowStepDto } from "@/shared/models/flow-step/flow-step-dto";
import type { FlowCreateDto } from "@/shared/models/flow/flow-create-dto";
import type { FlowDto } from "@/shared/models/flow/flow-dto";
import type { SubFlowCreateDto } from "@/shared/models/sub-flow/sub-flow-create-dto";
import type { SubFlowDto } from "@/shared/models/sub-flow/sub-flow-dto";

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
  greet: (name: string) => call<{ greeting: string }>("greet", { name }),

  Flow: {
    create: (dto: FlowCreateDto) =>
      call<{ newId: number; success: boolean }>("Flow.create", dto),
    get: (id: number) => call<FlowDto>("Flow.get", id),
    // getAll: () => call<Flow[]>("Flow.getAll"),
    getDataTable: (dto: DataTableDto) =>
      call<DataTableResponseDto<FlowDto>>("Flow.getDataTable", dto),
  },

  FlowStep: {
    create: (dto: FlowStepCreateDto) =>
      call<{ newId: number; success: boolean }>("FlowStep.create", dto),
    get: (id: number) => call<FlowStepDto>("FlowStep.get", id),
  },

  FlowStepImage: {
    create: (dto: FlowStepImageCreateDto) =>
      call<{ newId: number; success: boolean }>("FlowStepImage.create", dto),
    get: (id: number) => call<FlowStepImageDto>("FlowStepImage.get", id),
  },

  FlowSearchArea: {
    create: (dto: FlowSearchAreaCreateDto) =>
      call<{ newId: number; success: boolean }>("FlowSearchArea.create", dto),
    get: (id: number) => call<FlowSearchAreaDto>("FlowSearchArea.get", id),
  },

  SubFlow: {
    create: (dto: SubFlowCreateDto) =>
      call<{ newId: number; success: boolean }>("SubFlow.create", dto),
    get: (id: number) => call<SubFlowDto>("SubFlow.get", id),
  },
};

async function call<T = any>(action: string, payload: any = {}): Promise<T> {
  const msg: RequestMessage = {
    action,
    payload,
  };

  try {
    return await backendApi.invoke<T>(msg);
  } catch (err: any) {
    console.error(`Backend call failed [${action}]:`, err);
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
