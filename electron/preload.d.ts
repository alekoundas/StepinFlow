interface BackendApi {
  invoke: <T = unknown>(msg: any) => Promise<T>;
  // send: (msg: any) => void;
  onMessage: <T = any>(callback: (msg: T) => void) => () => void;
}

declare global {
  interface Window {
    backendApi: BackendApi;
  }
}

export {};
