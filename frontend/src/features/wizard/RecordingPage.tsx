import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { InputText } from "primereact/inputtext";
import { Panel } from "primereact/panel";
import { Tag } from "primereact/tag";

import {
  BroadcastTypeEnum,
  type RecordedInput,
} from "../../../../electron/shared/types";
import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { FlowDto } from "@/shared/models/database/flow-dto";
import { useFlowMutations } from "@/features/flow/hooks/use-flow";
import { useWizardStore } from "@/features/wizard/store/wizard-store";

/**
 * Records what the user does and shows it arriving.
 *
 * The feed is metadata only. A click screenshot is around 100KB and the backend keeps dozens of
 * them, so nothing here carries pixels; the wizard asks for an image when it draws that step.
 */
export default function RecordingPage() {
  const navigate = useNavigate();
  const { target, createAsSubFlow, setTarget, setActions, reset } = useWizardStore();
  const { createFlowMutation } = useFlowMutations();

  const [flowName, setFlowName] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [events, setEvents] = useState<RecordedInput[]>([]);
  const [error, setError] = useState<string | null>(null);

  const feedRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.RECORDING_EVENT) return;
      setEvents((previous) => [...previous, message.payload as RecordedInput]);
    });

    return unsubscribe;
  }, []);

  // Follow the tail, so the newest action is the one on screen.
  useEffect(() => {
    feedRef.current?.scrollTo({ top: feedRef.current.scrollHeight });
  }, [events]);

  const handleStart = async () => {
    setError(null);
    setEvents([]);

    try {
      await backendApiService.Recording.start();
      setIsRecording(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // The flow is created before anything is recorded, not on save. Every step form then has a
  // real flow to list search areas and points against, and to save a new one into, which is the
  // difference between an image search step you can finish here and one you cannot.
  const handleCreateFlow = async () => {
    if (flowName.trim().length === 0) return;

    setIsCreating(true);
    setError(null);

    try {
      const flowId = await createFlowMutation.mutateAsync(
        new FlowDto({ name: flowName.trim(), isSubFlow: createAsSubFlow }),
      );

      setTarget({ targetFlowId: flowId, targetIndex: 0 }, flowId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsCreating(false);
    }
  };

  const handleStop = async () => {
    setIsStopping(true);
    setError(null);

    try {
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

  const handleCancel = async () => {
    try {
      await backendApiService.Recording.discard();
    } catch (err) {
      console.error(err);
    }

    reset();
    navigate(-1);
  };

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
          {!target ? (
            <>
              <InputText
                value={flowName}
                onChange={(e) => setFlowName(e.target.value)}
                placeholder="Name the flow"
                className="w-14rem"
              />
              <Button
                type="button"
                label="Create and continue"
                icon="pi pi-check"
                loading={isCreating}
                disabled={flowName.trim().length === 0 || isCreating}
                onClick={handleCreateFlow}
              />
            </>
          ) : !isRecording ? (
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
          text="Recording. Switch to the app you want to automate; StepinFlow keeps listening in the background."
        />
      )}

      <Panel header={`Actions (${events.length})`}>
        <div
          ref={feedRef}
          className="flex flex-column gap-1 overflow-auto"
          style={{ maxHeight: "26rem" }}
        >
          {events.length === 0 && (
            <LabelComponent
              text={
                isRecording
                  ? "Waiting for your first action..."
                  : "Nothing recorded yet."
              }
              size="sm"
              color="secondary"
            />
          )}

          {events.map((event) => (
            <RecordedEventRow
              key={event.index}
              event={event}
            />
          ))}
        </div>
      </Panel>
    </div>
  );
}

function RecordedEventRow({ event }: { event: RecordedInput }) {
  return (
    <div className="flex align-items-center gap-2 p-2 border-round-sm surface-100">
      <IconComponent
        name={iconFor(event)}
        size="sm"
      />

      <LabelComponent
        text={describe(event)}
        size="sm"
      />

      {event.windowTitle && (
        <LabelComponent
          text={event.windowTitle}
          size="xs"
          color="secondary"
          wrap={false}
          className="overflow-hidden text-overflow-ellipsis"
        />
      )}

      {event.hasScreenshot && (
        <Tag
          severity="info"
          value="screenshot"
          className="ml-auto"
        />
      )}
    </div>
  );
}

const iconFor = (event: RecordedInput): string => {
  switch (event.type) {
    case "BUTTON_DOWN":
    case "BUTTON_UP":
      return "mouse";
    case "CURSOR_DRAG":
      return "arrows-alt";
    case "CURSOR_SCROLL":
      return "sort-alt";
    default:
      return "keyboard";
  }
};

const describe = (event: RecordedInput): string => {
  switch (event.type) {
    case "BUTTON_DOWN":
      return `Pressed at ${event.physicalX}, ${event.physicalY}`;
    case "BUTTON_UP":
      return `Released at ${event.physicalX}, ${event.physicalY}`;
    case "CURSOR_DRAG":
      return `Dragged to ${event.physicalX}, ${event.physicalY}`;
    case "CURSOR_SCROLL":
      return `Scrolled ${event.scrollDirection?.toLowerCase() ?? ""}`;
    case "KEY_DOWN":
      return `Key down ${event.keyCode ?? ""}`;
    case "KEY_UP":
      return `Key up ${event.keyCode ?? ""}`;
    default:
      return event.type;
  }
};
