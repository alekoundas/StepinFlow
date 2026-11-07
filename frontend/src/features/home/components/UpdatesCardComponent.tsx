import { Button } from "primereact/button";
import { ProgressBar } from "primereact/progressbar";
import { Skeleton } from "primereact/skeleton";

import StatusPillComponent, { type StatusPillSeverity } from "@/shared/components/StatusPillComponent";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { dayAndTime } from "@/shared/utils/day-and-time";
import { percentOf } from "@/features/ai/ai-format";
import { downloadedOf } from "@/features/auto-updater/auto-updater-format";
import { useAutoUpdaterStatus } from "@/features/auto-updater/hooks/use-auto-updater-status";
import { useIsExecuting } from "@/features/execution/hooks/use-is-executing";
import HomeCardComponent from "@/features/home/components/HomeCardComponent";
import StatTileComponent from "@/features/home/components/StatTileComponent";
import { MUTED } from "@/features/home/home-format";
import { AutoUpdaterStateEnum, type AutoUpdaterStatus } from "../../../../../electron/shared/types";

const ICON = "arrow-circle-up";
const TITLE = "Updates";

const DOWNLOADING_COLOR = "var(--blue-500)";
const READY_COLOR = "var(--yellow-500)";

const check = () => void ElectronApiService.autoUpdater.check();
const restart = () => void ElectronApiService.autoUpdater.restart();

const pillOf = (status: AutoUpdaterStatus): { text: string; severity: StatusPillSeverity; pulse: boolean } => {
  switch (status.state) {
    case AutoUpdaterStateEnum.UNAVAILABLE:
      return { text: "Not available", severity: "neutral", pulse: false };
    case AutoUpdaterStateEnum.DOWNLOADING:
      return { text: "Downloading", severity: "running", pulse: true };
    case AutoUpdaterStateEnum.READY:
      return { text: "Restart needed", severity: "paused", pulse: false };
    case AutoUpdaterStateEnum.FAILED:
      return { text: "Update failed", severity: "danger", pulse: false };
    default:
      // A check going on leaves it saying when the last one got an answer: the button shows this one.
      if (!status.lastCheckedAt) return { text: "Not checked yet", severity: "neutral", pulse: false };
      return { text: `Last check ${dayAndTime(status.lastCheckedAt)}`, severity: "success", pulse: false };
  }
};

const noteOf = (status: AutoUpdaterStatus, isExecuting: boolean): string => {
  switch (status.state) {
    case AutoUpdaterStateEnum.UNAVAILABLE:
      return "Only an installed app updates itself.";
    case AutoUpdaterStateEnum.CHECKING:
      return "Looking for a newer version.";
    case AutoUpdaterStateEnum.UP_TO_DATE:
      return "You have the latest version. It checks again every few hours.";
    case AutoUpdaterStateEnum.DOWNLOADING:
      return "It installs when you restart or close StepinFlow.";
    case AutoUpdaterStateEnum.READY:
      if (isExecuting) return "Stop or finish the execution first.";
      return "Restart now, or it installs when you close StepinFlow.";
    case AutoUpdaterStateEnum.FAILED:
      return "The last try failed. The log has the details.";
    default:
      return "It checks when it starts and every few hours.";
  }
};

/**
 * The version installed and what the auto-updater is doing about a newer one. It checks by itself, so
 * Check for updates is only for not waiting.
 */
export default function UpdatesCardComponent() {
  const status = useAutoUpdaterStatus();
  const isExecuting = useIsExecuting();

  if (!status)
    return (
      <HomeCardComponent
        icon={ICON}
        title={TITLE}
      >
        <Skeleton height="4.5rem" />
      </HomeCardComponent>
    );

  const pill = pillOf(status);
  const isChecking = status.state === AutoUpdaterStateEnum.CHECKING;
  const isDownloading = status.state === AutoUpdaterStateEnum.DOWNLOADING;
  const isReady = status.state === AutoUpdaterStateEnum.READY;
  const isSizeKnown = status.totalBytes > 0;

  return (
    <HomeCardComponent
      icon={ICON}
      title={TITLE}
      action={
        <StatusPillComponent
          text={pill.text}
          severity={pill.severity}
          pulse={pill.pulse}
        />
      }
    >
      <div className="flex gap-2">
        <StatTileComponent
          value={status.currentVersion}
          label="Installed"
        />

        {isDownloading && (
          <StatTileComponent
            value={status.version}
            label="Downloading"
            dotColor={DOWNLOADING_COLOR}
          />
        )}
        {isReady && (
          <StatTileComponent
            value={status.version}
            label="Ready to install"
            dotColor={READY_COLOR}
          />
        )}
      </div>

      {isDownloading && (
        <div className="flex flex-column gap-1">
          <ProgressBar
            value={percentOf(status.transferredBytes, status.totalBytes)}
            mode={isSizeKnown ? "determinate" : "indeterminate"}
            showValue={false}
            style={{ height: 6 }}
          />
          {isSizeKnown && (
            <span
              className="text-xs"
              style={MUTED}
            >
              {downloadedOf(status)}
            </span>
          )}
        </div>
      )}

      <div className="flex align-items-center justify-content-between flex-wrap gap-2">
        <span
          className="text-sm"
          style={MUTED}
        >
          {noteOf(status, isExecuting)}
        </span>

        <div className="flex align-items-center flex-none gap-1 ml-auto">
          {status.state === AutoUpdaterStateEnum.FAILED && (
            <Button
              type="button"
              label="Show log"
              size="small"
              severity="secondary"
              text
              onClick={() => void ElectronApiService.files.showInFolder(status.logPath)}
            />
          )}

          {isReady ? (
            <Button
              type="button"
              label="Restart to update"
              icon="pi pi-refresh"
              size="small"
              disabled={isExecuting}
              onClick={restart}
            />
          ) : (
            <Button
              type="button"
              label={isChecking ? "Checking..." : "Check for updates"}
              icon="pi pi-sync"
              size="small"
              severity="secondary"
              outlined
              loading={isChecking}
              disabled={status.state === AutoUpdaterStateEnum.UNAVAILABLE || isDownloading}
              onClick={check}
            />
          )}
        </div>
      </div>
    </HomeCardComponent>
  );
}
