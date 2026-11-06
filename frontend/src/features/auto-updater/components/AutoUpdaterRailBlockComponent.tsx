import { useEffect, useRef, type CSSProperties, type ReactNode } from "react";
import { Button } from "primereact/button";
import { ProgressBar } from "primereact/progressbar";
import { Tooltip } from "primereact/tooltip";

import { percentOf } from "@/features/ai/ai-format";
import { useAutoUpdaterStatus } from "@/features/auto-updater/hooks/use-auto-updater-status";
import { useIsExecuting } from "@/features/execution/hooks/use-is-executing";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { AutoUpdaterStateEnum, type AutoUpdaterStatus } from "../../../../../electron/shared/types";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const RING_RADIUS = 9;
const RING_LENGTH = 2 * Math.PI * RING_RADIUS;

const megabytes = (bytes: number): number => Math.round(bytes / 1024 / 1024);

const check = () => void ElectronApiService.autoUpdater.check();
const restart = () => void ElectronApiService.autoUpdater.restart();

/**
 * The auto-updater in the rail, shown only when there is something to say. On the narrow rail of the flow
 * and execution pages it is only an icon, with the words in its tooltip.
 */
export default function AutoUpdaterRailBlockComponent({ isNarrow }: { isNarrow: boolean }) {
  const status = useAutoUpdaterStatus();

  if (status?.state === AutoUpdaterStateEnum.CHECKING && status.isAskedCheck)
    return <CheckingComponent isNarrow={isNarrow} />;

  if (status?.state === AutoUpdaterStateEnum.DOWNLOADING)
    return (
      <DownloadingComponent
        status={status}
        isNarrow={isNarrow}
      />
    );

  if (status?.state === AutoUpdaterStateEnum.READY)
    return (
      <ReadyComponent
        status={status}
        isNarrow={isNarrow}
      />
    );

  if (status?.state === AutoUpdaterStateEnum.FAILED)
    return (
      <FailedComponent
        status={status}
        isNarrow={isNarrow}
      />
    );

  return null;
}

function CheckingComponent({ isNarrow }: { isNarrow: boolean }) {
  const title = "Checking for updates";

  if (isNarrow)
    return (
      <NarrowItemComponent tooltip={title}>
        <i
          className="pi pi-spin pi-spinner"
          aria-hidden="true"
        />
      </NarrowItemComponent>
    );

  return (
    <CardComponent
      icon="pi pi-spin pi-spinner"
      title={title}
    />
  );
}

function DownloadingComponent({ status, isNarrow }: { status: AutoUpdaterStatus; isNarrow: boolean }) {
  const title = `Downloading ${status.version}`;
  const isSizeKnown = status.totalBytes > 0;
  const amount = `${megabytes(status.transferredBytes)} of ${megabytes(status.totalBytes)} MB`;
  const percent = percentOf(status.transferredBytes, status.totalBytes);

  if (isNarrow)
    return (
      <NarrowItemComponent tooltip={isSizeKnown ? `${title} · ${amount}` : title}>
        <RingComponent percent={percent} />
      </NarrowItemComponent>
    );

  return (
    <CardComponent
      icon="pi pi-download"
      title={title}
    >
      <ProgressBar
        value={percent}
        mode={isSizeKnown ? "determinate" : "indeterminate"}
        showValue={false}
        style={{ height: 4 }}
      />
      {isSizeKnown && (
        <span
          className="text-xs"
          style={MUTED}
        >
          {amount}
        </span>
      )}
    </CardComponent>
  );
}

function ReadyComponent({ status, isNarrow }: { status: AutoUpdaterStatus; isNarrow: boolean }) {
  const isExecuting = useIsExecuting();
  const title = `${status.version} is ready`;

  if (isNarrow) {
    let tooltip = `${title}. Click to restart and update.`;
    if (isExecuting) tooltip = `${title}. Stop or finish the execution first.`;

    return (
      <NarrowItemComponent
        tooltip={tooltip}
        onClick={restart}
        isDisabled={isExecuting}
      >
        <span className="relative flex">
          <i
            className="pi pi-arrow-circle-up"
            style={{ color: "var(--primary-color)" }}
            aria-hidden="true"
          />
          <span
            className="absolute border-circle"
            style={{ width: 8, height: 8, top: -3, right: -4, background: "var(--green-400)" }}
          />
        </span>
      </NarrowItemComponent>
    );
  }

  return (
    <CardComponent
      icon="pi pi-arrow-circle-up"
      iconColor="var(--primary-color)"
      title={title}
    >
      <Button
        type="button"
        label="Restart to update"
        size="small"
        className="w-full py-2 white-space-nowrap"
        disabled={isExecuting}
        onClick={restart}
      />
      <span
        className="text-xs"
        style={MUTED}
      >
        {isExecuting ? "Stop or finish the execution first" : "Or it installs when you close StepinFlow"}
      </span>
    </CardComponent>
  );
}

function FailedComponent({ status, isNarrow }: { status: AutoUpdaterStatus; isNarrow: boolean }) {
  const title = "Update failed";

  if (isNarrow)
    return (
      <NarrowItemComponent
        tooltip={`${title}. Click to try again.`}
        onClick={check}
      >
        <i
          className="pi pi-exclamation-triangle"
          style={{ color: "var(--yellow-500)" }}
          aria-hidden="true"
        />
      </NarrowItemComponent>
    );

  return (
    <CardComponent
      icon="pi pi-exclamation-triangle"
      iconColor="var(--yellow-500)"
      title={title}
    >
      <div className="flex gap-2">
        <Button
          type="button"
          label="Try again"
          size="small"
          severity="secondary"
          outlined
          className="flex-1 px-2 py-2 white-space-nowrap"
          onClick={check}
        />
        <Button
          type="button"
          label="Show log"
          size="small"
          severity="secondary"
          text
          className="flex-1 px-2 py-2 white-space-nowrap"
          onClick={() => void ElectronApiService.files.showInFolder(status.logPath)}
        />
      </div>
    </CardComponent>
  );
}

function CardComponent({
  icon,
  iconColor,
  title,
  children,
}: {
  icon: string;
  iconColor?: string;
  title: string;
  children?: ReactNode;
}) {
  return (
    <div
      className="flex flex-column gap-2 mb-1 p-2 border-1 border-round-lg"
      style={{ borderColor: "var(--surface-border)", background: "var(--surface-card)" }}
    >
      <span
        className="flex align-items-center gap-2 text-sm font-medium"
        style={{ color: "var(--text-color)" }}
      >
        <i
          className={icon}
          style={{ color: iconColor }}
          aria-hidden="true"
        />
        {title}
      </span>
      {children}
    </div>
  );
}

function NarrowItemComponent({
  tooltip,
  onClick,
  isDisabled = false,
  children,
}: {
  tooltip: string;
  onClick?: () => void;
  isDisabled?: boolean;
  children: ReactNode;
}) {
  const tooltipRef = useRef<Tooltip>(null);
  const isClickable = !!onClick && !isDisabled;

  // A tooltip only looks for its targets when it re-renders, and this target has just appeared.
  useEffect(() => {
    tooltipRef.current?.updateTargetEvents();
  }, []);

  return (
    <>
      <Tooltip
        ref={tooltipRef}
        target=".auto-updater-rail-item"
        position="right"
      />
      {/* Not disabled, which would stop the tooltip that says why. */}
      <button
        type="button"
        onClick={isClickable ? onClick : undefined}
        className="auto-updater-rail-item flex align-items-center justify-content-center flex-none border-none border-round-lg p-0"
        style={{
          width: 44,
          height: 44,
          background: "transparent",
          cursor: isClickable ? "pointer" : "default",
          opacity: isDisabled ? 0.6 : 1,
          ...MUTED,
        }}
        aria-label={tooltip}
        aria-disabled={!isClickable}
        data-pr-tooltip={tooltip}
      >
        {children}
      </button>
    </>
  );
}

function RingComponent({ percent }: { percent: number }) {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 22 22"
      style={{ transform: "rotate(-90deg)" }}
      aria-hidden="true"
    >
      <circle
        cx="11"
        cy="11"
        r={RING_RADIUS}
        fill="none"
        stroke="var(--surface-border)"
        strokeWidth="2.5"
      />
      <circle
        cx="11"
        cy="11"
        r={RING_RADIUS}
        fill="none"
        stroke="var(--primary-color)"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeDasharray={RING_LENGTH}
        strokeDashoffset={RING_LENGTH * (1 - percent / 100)}
      />
    </svg>
  );
}
