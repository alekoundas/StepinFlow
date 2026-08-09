import type { ElectronApi } from "../../../../electron/shared/electron-api";

// The only place window.electronApi is declared. Preload implements the same interface, so a
// change on either side is a compile error rather than a runtime undefined.
declare global {
  interface Window {
    electronApi: ElectronApi;
  }
}

export {};
