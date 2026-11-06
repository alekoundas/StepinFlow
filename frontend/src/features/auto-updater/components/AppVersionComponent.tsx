import type { CSSProperties } from "react";
import { Button } from "primereact/button";

import { useAutoUpdaterStatus } from "@/features/auto-updater/hooks/use-auto-updater-status";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { AutoUpdaterStateEnum, type AutoUpdaterStatus } from "../../../../../electron/shared/types";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const noteOf = (status: AutoUpdaterStatus): string => {
  switch (status.state) {
    case AutoUpdaterStateEnum.UNAVAILABLE:
      return "Only an installed app updates itself.";
    case AutoUpdaterStateEnum.UP_TO_DATE:
      return "You have the latest version.";
    case AutoUpdaterStateEnum.DOWNLOADING:
      return `Downloading ${status.version}.`;
    case AutoUpdaterStateEnum.READY:
      return `${status.version} is ready to install.`;
    case AutoUpdaterStateEnum.FAILED:
      return "The last try failed. The log has the details.";
    default:
      return "";
  }
};

/** The version this is, and a check for a newer one. What a check finds shows in the rail. */
export default function AppVersionComponent() {
  const status = useAutoUpdaterStatus();

  if (!status) return null;

  const isChecking = status.state === AutoUpdaterStateEnum.CHECKING;
  const note = noteOf(status);

  return (
    <div
      className="flex flex-column align-items-start gap-2 mt-3 pt-3 border-top-1"
      style={{ paddingInline: 12, borderColor: "var(--surface-border)" }}
    >
      <span
        className="text-sm"
        style={MUTED}
      >
        StepinFlow {status.currentVersion}
      </span>

      <Button
        type="button"
        label={isChecking ? "Checking..." : "Check for updates"}
        icon="pi pi-refresh"
        size="small"
        severity="secondary"
        outlined
        className="white-space-nowrap"
        loading={isChecking}
        disabled={
          status.state === AutoUpdaterStateEnum.UNAVAILABLE ||
          status.state === AutoUpdaterStateEnum.DOWNLOADING ||
          status.state === AutoUpdaterStateEnum.READY
        }
        onClick={() => void ElectronApiService.autoUpdater.check()}
      />

      {note && (
        <span
          className="text-xs"
          style={MUTED}
        >
          {note}
        </span>
      )}
    </div>
  );
}
