import { useEffect, useState } from "react";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { AutoUpdaterStatus } from "../../../../../electron/shared/types";

/** What the auto-updater is doing, which Electron's main process keeps. Null until it has answered. */
export function useAutoUpdaterStatus(): AutoUpdaterStatus | null {
  const [status, setStatus] = useState<AutoUpdaterStatus | null>(null);

  useEffect(() => {
    const unsubscribe = ElectronApiService.autoUpdater.onStatusChange(setStatus);
    void ElectronApiService.autoUpdater.getStatus().then(setStatus);

    return unsubscribe;
  }, []);

  return status;
}
