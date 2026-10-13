import { useEffect, useLayoutEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";
import { useSearchParams } from "react-router-dom";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { classNames } from "primereact/utils";

import { BroadcastTypeEnum } from "../../../../electron/shared/types";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import type { RecordingProgressDto } from "@/shared/models/recording-progress-dto";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import RecordingLiveListComponent from "@/features/wizard/components/RecordingLiveListComponent";
import { toClock } from "@/shared/utils/clock";
import { toHotkeyDisplay } from "@/shared/utils/hotkey";

// The list shows the newest few, as many as fit without scrolling.
const LISTED_ACTIONS = 6;

// Where the window can be dragged from. Electron reads it from CSS; React has no type for it.
const DRAG = { WebkitAppRegion: "drag" } as CSSProperties;

/**
 * The floating bar a recording is controlled from: frameless, always on top, and left out of
 * every screenshot. Its own window, so nothing done on it is recorded, and it never takes the
 * focus, so the keyboard stays with the application being recorded.
 *
 * It only asks. Pause, resume and stop go to the backend, which tells every window what changed;
 * the recording page takes a stop or a discard from there, wherever it came from - here, a hotkey,
 * or the page itself - and closes this window.
 *
 * The window is sized to what is drawn, so no see-through margin sits over the application
 * taking its clicks.
 */
export default function RecordingBarPage() {
  const [searchParams] = useSearchParams();
  const flowIdParam = searchParams.get("flowId");
  const flowId = flowIdParam ? Number(flowIdParam) : undefined;

  const { data: settings = [] } = useAppSettings();

  const [progress, setProgress] = useState<RecordingProgressDto | null>(null);
  const [receivedAt, setReceivedAt] = useState(() => Date.now());
  const [now, setNow] = useState(() => Date.now());
  const [isListOpen, setIsListOpen] = useState(true);
  const [isBusy, setIsBusy] = useState(false);
  const [isConfirmingDiscard, setIsConfirmingDiscard] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const rootRef = useRef<HTMLDivElement>(null);
  const hasProgressRef = useRef(false);

  // Asked once, because the bar opens after the recording started, then told of every change.
  useEffect(() => {
    const receive = (received: RecordingProgressDto) => {
      hasProgressRef.current = true;
      setProgress(received);
      setReceivedAt(Date.now());
    };

    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.RECORDING_PROGRESS) return;
      receive(message.payload as RecordingProgressDto);
    });

    backendApiService.Recording.getProgress()
      .then((received) => {
        if (!hasProgressRef.current) receive(received);
      })
      .catch((err) => setError(err instanceof Error ? err.message : String(err)));

    return unsubscribe;
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 500);
    return () => window.clearInterval(timer);
  }, []);

  useLayoutEffect(() => {
    const root = rootRef.current;
    if (!root) return;

    const observer = new ResizeObserver(() => {
      const bounds = root.getBoundingClientRect();
      ElectronApiService.recordingBar.resizeWindow(bounds.width, bounds.height);
    });

    observer.observe(root);
    return () => observer.disconnect();
  }, []);

  // A discard asks twice. One stray click would lose the whole recording.
  useEffect(() => {
    if (!isConfirmingDiscard) return;

    const timer = window.setTimeout(() => setIsConfirmingDiscard(false), 3000);
    return () => window.clearTimeout(timer);
  }, [isConfirmingDiscard]);

  const state = progress?.state ?? RecordingStateEnum.RECORDING;
  const isPaused = state === RecordingStateEnum.PAUSED;
  const isLive = state === RecordingStateEnum.RECORDING || isPaused;

  const elapsed = (progress?.elapsedMilliseconds ?? 0) + (isLive ? now - receivedAt : 0);
  const actionCount = progress?.actionCount ?? 0;
  const actions = (progress?.recentActions ?? []).slice(-LISTED_ACTIONS);
  const lastLine = actions[actions.length - 1]?.line ?? "Waiting for your first action...";

  const pauseKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_PAUSE)?.value;
  const stopKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_STOP)?.value;

  const ask = async (request: () => Promise<unknown>) => {
    setError(null);
    setIsBusy(true);

    try {
      await request();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsBusy(false);
    }
  };

  const togglePause = () =>
    void ask(() => (isPaused ? backendApiService.Recording.resume() : backendApiService.Recording.pause()));

  const stop = () => void ask(() => backendApiService.Recording.stop());

  const discard = () => {
    if (!isConfirmingDiscard) {
      setIsConfirmingDiscard(true);
      return;
    }

    void ask(() => backendApiService.Recording.discard(flowId));
  };

  const accent = isPaused ? "var(--yellow-500)" : "var(--red-400)";

  return (
    <div
      ref={rootRef}
      className="flex flex-column gap-2 p-1 select-none"
      style={{ width: "max-content" }}
    >
      <div
        role="toolbar"
        aria-label="Recording"
        className="flex align-items-center gap-1 p-1 border-round-xl surface-overlay border-1 surface-border"
      >
        <div
          className="flex align-items-center gap-2 px-3 border-round-lg"
          style={{
            ...DRAG,
            height: 36,
            background: `color-mix(in srgb, ${accent} 14%, transparent)`,
          }}
        >
          <span
            className={classNames("border-circle", !isPaused && "status-pill-pulse")}
            style={{ width: 10, height: 10, background: accent }}
          />
          <span
            className="text-xs font-bold"
            style={{ color: accent, letterSpacing: "0.08em" }}
          >
            {isPaused ? "PAUSED" : "REC"}
          </span>
          <span
            className="text-sm"
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            {toClock(elapsed)}
          </span>
        </div>

        <div
          className="flex flex-column justify-content-center px-3"
          style={{ ...DRAG, height: 36, width: 260 }}
        >
          <LabelComponent
            text={actionCount === 1 ? "1 action" : `${actionCount} actions`}
            size="xs"
            color="secondary"
          />
          <span className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {lastLine}
          </span>
        </div>

        <span
          aria-hidden="true"
          className="mx-1"
          style={{ width: 1, height: 24, background: "var(--surface-border)" }}
        />

        <Button
          type="button"
          icon={isPaused ? "pi pi-play" : "pi pi-pause"}
          label={isPaused ? "Resume" : "Pause"}
          onClick={togglePause}
          disabled={isBusy || !isLive}
          className="p-button-sm p-button-secondary gap-1"
          style={{ minWidth: "7.5rem" }}
        >
          {pauseKeys && <HotkeyComponent keys={pauseKeys} />}
        </Button>

        <Button
          type="button"
          icon="pi pi-stop"
          label="Stop"
          onClick={stop}
          disabled={isBusy || !isLive}
          className="p-button-sm gap-1"
        >
          {stopKeys && <HotkeyComponent keys={stopKeys} />}
        </Button>

        <Button
          type="button"
          icon={isListOpen ? "pi pi-chevron-up" : "pi pi-chevron-down"}
          aria-label={isListOpen ? "Hide the action list" : "Show the action list"}
          aria-expanded={isListOpen}
          onClick={() => setIsListOpen(!isListOpen)}
          className="p-button-sm p-button-text p-button-plain"
        />

        <Button
          type="button"
          icon="pi pi-times"
          label={isConfirmingDiscard ? "Discard?" : undefined}
          aria-label="Discard the recording"
          onClick={discard}
          disabled={isBusy}
          className={classNames(
            "p-button-sm p-button-text",
            isConfirmingDiscard ? "p-button-danger" : "p-button-plain",
          )}
        />
      </div>

      {isListOpen && (
        <div className="flex flex-column gap-1 p-2 border-round-xl surface-overlay border-1 surface-border">
          {actions.length === 0 ? (
            <LabelComponent
              text="Nothing recorded yet."
              size="sm"
              color="secondary"
              className="px-2 py-1"
            />
          ) : (
            <RecordingLiveListComponent actions={actions} />
          )}

          {isPaused && (
            <Message
              severity="warn"
              className="w-full justify-content-start"
              text="Paused. Nothing you do is recorded until you resume."
            />
          )}

          {error && (
            <Message
              severity="error"
              className="w-full justify-content-start"
              text={error}
            />
          )}
        </div>
      )}
    </div>
  );
}

function HotkeyComponent({ keys }: { keys: string }) {
  return (
    <kbd
      className="text-xs border-1 border-round px-1 ml-1"
      style={{ fontFamily: "inherit", opacity: 0.8 }}
    >
      {toHotkeyDisplay(keys)}
    </kbd>
  );
}
