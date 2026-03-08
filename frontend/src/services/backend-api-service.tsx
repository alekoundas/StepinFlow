import { useEffect } from "react";

type MessageCallback<T = any> = (msg: T) => void;

const backendApi = window.backendApi;
const pendingRequests = new Map<string, PendingRequest>(); 

if (!backendApi) {
  console.error("backendApi not available — preload missing?");
}

export const backend = {
  greet: (name: string) => request<{ greeting: string }>("greet", name),
  test: () => request<{ testResponse: string }>("test"),
};
interface RequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Match requests with responses
}

export interface ResponseMessage {
  action: string;
  payload: unknown;
  correlationId: string;
  error?: string;
}

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (error: any) => void;
  timeoutId?: NodeJS.Timeout;
}


// Listen **once** for all .NET responses
export function setupResponseListener() {
  backendApi.onMessage((rawMsg: any) => {
    const msg = rawMsg as ResponseMessage;
    const pending = pendingRequests.get(msg.correlationId);

    if (!pending) {
      console.warn(
        "Received response for unknown correlationId:",
        msg.correlationId,
      );
      return;
    }

    pendingRequests.delete(msg.correlationId);
    clearTimeout(pending.timeoutId);

    if (msg.error) {
      pending.reject(new Error(msg.error));
    } else {
      pending.resolve(msg.payload);
    }
  });
}

// Request from .NET
export async function request<T = any>(
  action: string,
  payload: any = {},
  timeoutMs = 30000,
): Promise<T> {
  const correlationId = crypto.randomUUID();

  const msg: RequestMessage = {
    action,
    payload,
    correlationId,
  };

  return new Promise((resolve, reject) => {
    const timeoutId = setTimeout(() => {
      pendingRequests.delete(correlationId);
      reject(new Error(`Request timed out after ${timeoutMs}ms`));
    }, timeoutMs);

    pendingRequests.set(correlationId, { resolve, reject, timeoutId });
    backendApi.invoke(msg).catch(reject); // just to catch send errors
  });
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
