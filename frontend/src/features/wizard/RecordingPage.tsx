import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { ProgressBar } from "primereact/progressbar";

import { BroadcastTypeEnum } from "../../../../electron/shared/types";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import { RecordingStageEnum } from "@/shared/enums/backend/recording-stage-enum";
import type { RecordingProgressDto } from "@/shared/models/recording-progress-dto";
import RecordingSetupComponent from "@/features/wizard/components/RecordingSetupComponent";
import RecordingLiveListComponent from "@/features/wizard/components/RecordingLiveListComponent";
import { useWizardStore } from "@/features/wizard/store/wizard-store";

const STAGE_LABELS: Record<RecordingStageEnum, string> = {
  NONE: "Getting the review ready",
  FINDING_ELEMENTS: "Finding what you clicked",
};

/**
 * Records what the user does and shows it arriving, folded into actions.
 *
 * Record now opens the recording bar and minimises this window, and the bar's Start starts the
 * recording, so the tester can arrange their windows first.
 *
 * The page only asks; the backend says what happened, as RECORDING_PROGRESS, and the page follows
 * it. A stop from here, the bar or its hotkey is the same stop: the backend works the recording
 * through, stage by stage, then says READY, and the page fetches the result and opens the wizard.
 * A discard from anywhere comes back as NONE, and the page leaves. The bar closes once the
 * recording stops.
 *
 * The live list is lines, never pixels; the wizard asks for an image when it draws that step.
 */
export default function RecordingPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const {
    target,
    createAsSubFlow,
    setupFlowId,
    setTarget,
    setSetupFlowId,
    setActions,
    setClickedElements,
    reset,
  } = useWizardStore();

  // The recording this page follows, null until one is underway, so the NONE of a discard made
  // before any recording - this page's own Cancel - is not taken for one.
  const [progress, setProgress] = useState<RecordingProgressDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.RECORDING_PROGRESS) return;

      const received = message.payload as RecordingProgressDto;
      setProgress((current) => (current || isUnderway(received) ? received : current));
    });

    return unsubscribe;
  }, []);

  // The bar never outlives the page that opened it.
  useEffect(() => {
    return () => {
      void ElectronApiService.recordingBar.closeWindow();
    };
  }, []);

  const leave = async () => {
    await ElectronApiService.recordingBar.closeWindow();
    await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });

    reset();
    navigate(-1);
  };

  const openReview = async (ready: RecordingProgressDto) => {
    if (ready.errorMessage) console.error(ready.errorMessage);

    try {
      const recording = await backendApiService.Recording.getResult();

      if (recording.actions.length === 0) {
        setProgress(null);
        setError("Nothing was recorded. Start again and perform the task you want automated.");
        return;
      }

      setActions(recording.actions);
      setClickedElements(recording.clickedElements);
      navigate("/wizard");
    } catch (err) {
      setProgress(null);
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // Stopped or discarded, from wherever it was.
  useEffect(() => {
    if (!progress || isLive(progress)) return;

    void ElectronApiService.recordingBar.closeWindow();

    if (progress.state === RecordingStateEnum.READY) void openReview(progress);
    if (progress.state === RecordingStateEnum.NONE) void leave();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [progress?.state]);

  const handleRecordNow = async () => {
    setError(null);

    try {
      await ElectronApiService.recordingBar.openWindow(setupFlowId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // The flow is created before anything is recorded, not on save. Every step form then has a
  // real flow to list search areas and points against, and to save a new one into, which is the
  // difference between an image search step you can finish here and one you cannot. The recorded
  // steps go after the launch step when there is one.
  const handleSetupCreated = (flowId: number, addsLaunchStep: boolean) => {
    setTarget({ targetFlowId: flowId, targetIndex: addsLaunchStep ? 1 : 0 }, flowId);
    setSetupFlowId(flowId);
    void queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
  };

  const handleStop = async () => {
    setError(null);

    try {
      await backendApiService.Recording.stop();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // Following a recording, the page leaves on the NONE the discard comes back as.
  const handleCancel = async () => {
    const isFollowing = progress !== null;

    try {
      await ElectronApiService.recordingBar.closeWindow();
      await backendApiService.Recording.discard(setupFlowId);
    } catch (err) {
      console.error(err);
      await leave();
      return;
    }

    if (!isFollowing) await leave();
  };

  const isRecording = progress !== null && isLive(progress);
  const isStopped = progress !== null && !isLive(progress);
  const actionCount = progress?.actionCount ?? 0;
  const recentActions = progress?.recentActions ?? [];

  return (
    <div className="m-4 flex flex-column gap-3">
      <div className="flex align-items-center justify-content-between">
        <div className="flex flex-column">
          <LabelComponent
            text="Record a flow"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text="Do the task once. Every click and keystroke becomes a step you can correct before saving."
            size="sm"
            color="secondary"
          />
        </div>

        <div className="flex gap-2">
          {target && !progress && (
            <Button
              type="button"
              label="Record now"
              icon="pi pi-circle-fill"
              onClick={handleRecordNow}
              className="p-button-danger"
            />
          )}

          {target && progress && (
            <Button
              type="button"
              label={isRecording ? "Stop and review" : "Getting the review ready..."}
              icon="pi pi-stop-circle"
              loading={isStopped}
              onClick={handleStop}
            />
          )}

          <Button
            type="button"
            label="Cancel"
            onClick={handleCancel}
            className="p-button-text"
          />
        </div>
      </div>

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      {isRecording && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text="Recording. The bar at the top of the screen pauses and stops it, and nothing done on it is recorded."
        />
      )}

      {progress && isStopped && (
        <div className="flex flex-column gap-2">
          <LabelComponent
            text={describeStage(progress)}
            size="sm"
            color="secondary"
          />
          <ProgressBar
            value={percentOf(progress)}
            // A stage that cannot be counted ahead moves rather than sitting at zero.
            mode={progress.stageTotal ? "determinate" : "indeterminate"}
            showValue={false}
            style={{ height: "0.5rem" }}
          />
        </div>
      )}

      {!target && (
        <RecordingSetupComponent
          isSubFlow={createAsSubFlow}
          onCreated={handleSetupCreated}
        />
      )}

      {target && (
        <Panel header={`Actions (${actionCount})`}>
          {recentActions.length === 0 ? (
            <LabelComponent
              text={isRecording ? "Waiting for your first action..." : "Nothing recorded yet."}
              size="sm"
              color="secondary"
            />
          ) : (
            <RecordingLiveListComponent actions={recentActions} />
          )}
        </Panel>
      )}
    </div>
  );
}

function isLive(progress: RecordingProgressDto): boolean {
  return progress.state === RecordingStateEnum.RECORDING || progress.state === RecordingStateEnum.PAUSED;
}

function isUnderway(progress: RecordingProgressDto): boolean {
  return isLive(progress) || progress.state === RecordingStateEnum.PROCESSING;
}

// "Finding what you clicked: 44 of 90", and which stage of how many once there is more than one.
function describeStage(progress: RecordingProgressDto): string {
  let line = STAGE_LABELS[progress.stage];
  if (progress.stageTotal) line += `: ${progress.stageDone} of ${progress.stageTotal}`;
  if (progress.stageCount > 1) line = `${progress.stageNumber} of ${progress.stageCount} · ${line}`;

  return line;
}

function percentOf(progress: RecordingProgressDto): number {
  if (!progress.stageTotal) return 0;
  return Math.round((progress.stageDone * 100) / progress.stageTotal);
}
