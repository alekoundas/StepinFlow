import { useEffect, useState, type ReactNode } from "react";
import { Button } from "primereact/button";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { BackendStateEnum, type BackendStatus } from "../../../../electron/shared/types";

const STARTING: BackendStatus = {
  state: BackendStateEnum.STARTING,
  hadStarted: false,
  reason: "",
  logPath: "",
};

/**
 * Holds the app back until the backend can answer, since every page asks it for something as it
 * opens. Until then the window shows the startup screen in index.html.
 */
export default function BackendGateComponent({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState(STARTING);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnStatusChange(setStatus);
    void ElectronApiService.backendApi.GetStatus().then(setStatus);

    return unsubscribe;
  }, []);

  useEffect(() => {
    if (status.state !== BackendStateEnum.STARTING) document.getElementById("startup-screen")?.remove();
  }, [status.state]);

  if (status.state === BackendStateEnum.STARTING) return null;
  if (status.state === BackendStateEnum.STOPPED) return <BackendStoppedComponent status={status} />;

  return children;
}

function BackendStoppedComponent({ status }: { status: BackendStatus }) {
  let title = "StepinFlow could not start";
  if (status.hadStarted) title = "StepinFlow stopped unexpectedly";

  return (
    <div
      role="alert"
      className="flex align-items-center justify-content-center h-screen p-4 text-color"
    >
      <div
        className="flex flex-column gap-3 w-full"
        style={{ maxWidth: 640 }}
      >
        <div className="flex align-items-center gap-2">
          <i
            className="pi pi-exclamation-triangle text-xl"
            style={{ color: "var(--red-400)" }}
            aria-hidden="true"
          />
          <span className="text-xl font-semibold">{title}</span>
        </div>

        <span style={{ color: "var(--text-color-secondary)" }}>
          Trying again restarts the app. If this keeps happening, the log has the details.
        </span>

        <pre
          className="surface-ground border-1 surface-border border-round p-3 text-xs overflow-auto m-0 white-space-pre-wrap"
          style={{ maxHeight: "40vh" }}
        >
          {status.reason}
        </pre>

        <div className="flex gap-2">
          <Button
            label="Try again"
            icon="pi pi-refresh"
            onClick={() => void ElectronApiService.electronProcess.relaunch()}
          />
          <Button
            label="Show log"
            icon="pi pi-folder-open"
            severity="secondary"
            outlined
            onClick={() => void ElectronApiService.files.showInFolder(status.logPath)}
          />
        </div>
      </div>
    </div>
  );
}
