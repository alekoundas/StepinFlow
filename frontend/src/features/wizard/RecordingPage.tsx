import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";

import { BroadcastTypeEnum } from "../../../../electron/shared/types";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import type { RecordingProgressDto } from "@/shared/models/recording-progress-dto";
import RecordingSetupComponent from "@/features/wizard/components/RecordingSetupComponent";
import RecordingLiveListComponent from "@/features/wizard/components/RecordingLiveListComponent";
import { useWizardStore } from "@/features/wizard/store/wizard-store";

/**
 * Records what the user does and shows it arriving, folded into actions.
 *
 * While recording, this window is minimised and the recording bar controls it. A stop or a
 * discard can come from the bar, its hotkey or this page; whichever it was, the backend says so
 * and this page takes it from there - on to the wizard, or back out - and closes the bar.
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
    reset,
  } = useWizardStore();

  const [isRecording, setIsRecording] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [progress, setProgress] = useState<RecordingProgressDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Whether a stop or a discard broadcast is this page's to act on. Cleared before the page
  // stops or discards itself, so it does not act twice.
  const isRecordingRef = useRef(false);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.RECORDING_PROGRESS) return;
      setProgress(message.payload as RecordingProgressDto);
    });

    return unsubscribe;
  }, []);

  // The bar never outlives the page that opened it.
  useEffect(() => {
    return () => {
      void ElectronApiService.recordingBar.closeWindow();
    };
  }, []);

  const finish = async () => {
    setIsStopping(true);
    setError(null);

    try {
      await ElectronApiService.recordingBar.closeWindow();
      const recorded = await backendApiService.Recording.stop();
      setIsRecording(false);

      if (recorded.length === 0) {
        setError("Nothing was recorded. Start again and perform the task you want automated.");
        return;
      }

      setActions(recorded);
      navigate("/wizard");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsStopping(false);
    }
  };

  const leave = async () => {
    await ElectronApiService.recordingBar.closeWindow();
    await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });

    reset();
    navigate(-1);
  };

  // Stopped or discarded from the bar or a hotkey.
  useEffect(() => {
    if (!progress || !isRecordingRef.current) return;

    if (progress.state === RecordingStateEnum.STOPPED) {
      isRecordingRef.current = false;
      void finish();
    }

    if (progress.state === RecordingStateEnum.NONE) {
      isRecordingRef.current = false;
      void leave();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [progress]);

  const handleStart = async () => {
    setError(null);
    setProgress(null);

    try {
      await backendApiService.Recording.start(setupFlowId);
      isRecordingRef.current = true;
      setIsRecording(true);

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
    isRecordingRef.current = false;
    await finish();
  };

  const handleCancel = async () => {
    isRecordingRef.current = false;

    try {
      await ElectronApiService.recordingBar.closeWindow();
      await backendApiService.Recording.discard(setupFlowId);
      await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
    } catch (err) {
      console.error(err);
    }

    reset();
    navigate(-1);
  };

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
          {!target ? null : !isRecording ? (
            <Button
              type="button"
              label="Start recording"
              icon="pi pi-circle-fill"
              onClick={handleStart}
              className="p-button-danger"
            />
          ) : (
            <Button
              type="button"
              label={isStopping ? "Building steps..." : "Stop and review"}
              icon="pi pi-stop-circle"
              loading={isStopping}
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
