import { useEffect, useEffectEvent, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { Dropdown } from "primereact/dropdown";

import {
  BroadcastTypeEnum,
  type IpcBroadcastMessage,
  KeyCodeEnum,
  type RecordedInput,
  RecordedInputTypeEnum,
} from "../../../../../../../electron/shared/types";
import LabelComponent from "@/shared/components/LabelComponent";
import { ChoiceCardComponent } from "@/shared/components/ChoiceCardComponent";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import {
  stepsSearchingFor,
  useFlowStepUses,
  useFlowTemplates,
} from "@/features/workflow/hooks/use-flow-parts";

interface Props {
  flowId: number;
  stepName: string;
  /** The pictures already on the step, which it cannot take twice. */
  takenIds: number[];

  onCapture: () => void;
  onReuse: (template: FlowTemplateDto) => void;

  onClose?: () => void; // injected by DialogRootComponent
}

/**
 * How to add a template: capture a new picture, or link one the flow already has. Linking is what
 * lets a fix to a picture reach every step that looks for it.
 */
export function FlowTemplateAddDialogComponent({
  flowId,
  stepName,
  takenIds,
  onCapture,
  onReuse,
  onClose,
}: Props) {
  const [isReusing, setIsReusing] = useState(false);
  const [pickedId, setPickedId] = useState<number | null>(null);

  const { data: templates } = useFlowTemplates(flowId);
  const { data: uses } = useFlowStepUses(flowId);

  const options = (templates ?? []).filter((x) => !takenIds.includes(x.id));
  const picked = options.find((x) => x.id === pickedId);

  const capture = () => {
    onClose?.();
    onCapture();
  };

  const captureByKey = useEffectEvent(() => capture());

  // F3 through the global hook, whichever window has the keyboard, so the cursor stays on a hover
  // or a tooltip. Stopped before the capture, since the overlay cannot listen while this does.
  useEffect(() => {
    if (isReusing) return;

    let isListening = true;
    let unsubscribe: (() => void) | null = null;

    const stop = () => {
      if (!isListening) return;
      isListening = false;

      unsubscribe?.();
      backendApiService.System.inputRecordHotkeyStop().catch(() => {});
    };

    unsubscribe = backendApiService.OnBroadcast((event: IpcBroadcastMessage<RecordedInput>) => {
      if (event.type !== BroadcastTypeEnum.HOTKEY_CAPTURE_EVENT) return;
      if (event.payload.type !== RecordedInputTypeEnum.KEY_DOWN || event.payload.keyCode !== KeyCodeEnum.F3) return;

      stop();
      captureByKey();
    });

    backendApiService.System.inputRecordHotkeyStart().catch(() => {});

    return stop;
  }, [isReusing]);

  const reuse = () => {
    if (!picked) return;

    onClose?.();
    onReuse(picked);
  };

  const thumbnail = (template: FlowTemplateDto) => (
    <span
      className="flex align-items-center justify-content-center border-round surface-ground overflow-hidden flex-shrink-0"
      style={{ width: "3.5rem", height: "2.25rem" }}
    >
      {template.templateImage && (
        <img
          src={`data:image/png;base64,${template.templateImage}`}
          alt=""
          className="max-w-full max-h-full"
        />
      )}
    </span>
  );

  const usedByText = (template: FlowTemplateDto) => {
    const names = stepsSearchingFor(uses, template.name);
    if (names.length === 0) return "No step uses it yet";

    return `Used by ${names.join(", ")}`;
  };

  return (
    <Dialog
      visible
      header={isReusing ? "Reuse a template" : "Add a template"}
      onHide={() => onClose?.()}
      style={{ width: "38rem" }}
      draggable={false}
      footer={
        isReusing ? (
          <div className="flex justify-content-end gap-2">
            <Button
              type="button"
              label="Back"
              text
              severity="secondary"
              onClick={() => setIsReusing(false)}
            />
            <Button
              type="button"
              label="Add to step"
              disabled={!picked}
              onClick={reuse}
            />
          </div>
        ) : undefined
      }
    >
      {!isReusing && (
        <div className="flex flex-column gap-3">
          <LabelComponent
            text={`Another picture for ${stepName || "this step"} to look for.`}
            size="sm"
            color="secondary"
          />

          <ChoiceCardComponent
            icon="crosshairs"
            title="Capture now (F3)"
            description="Draw a box around it on the screen. It becomes a new template of this flow. To keep a hover or a tooltip in the picture, point at it and press F3 instead of clicking here."
            onSelect={capture}
          />

          {options.length > 0 && (
            <ChoiceCardComponent
              icon="link"
              title={`Reuse a template · ${options.length} in this flow`}
              description="Pick one this flow already has. This step links to it, so fixing the picture once fixes every step that uses it."
              onSelect={() => setIsReusing(true)}
            >
              <div className="flex gap-1 flex-wrap mt-1">
                {options.slice(0, 8).map((x) => (
                  <span key={x.id}>{thumbnail(x)}</span>
                ))}
              </div>
            </ChoiceCardComponent>
          )}
        </div>
      )}

      {isReusing && (
        <div className="flex flex-column gap-2">
          <LabelComponent
            text="Template"
            weight="bold"
          />
          <Dropdown
            value={pickedId}
            options={options}
            optionValue="id"
            optionLabel="name"
            placeholder="Pick a template…"
            onChange={(e) => setPickedId(e.value)}
            className="w-full"
            itemTemplate={(x: FlowTemplateDto) => (
              <div className="flex align-items-center gap-3">
                {thumbnail(x)}
                <div className="flex flex-column min-w-0">
                  <span style={{ fontFamily: "monospace" }}>{x.name}</span>
                  <LabelComponent
                    text={usedByText(x)}
                    size="xs"
                    color="secondary"
                  />
                </div>
              </div>
            )}
            valueTemplate={(x: FlowTemplateDto | null, props) =>
              x ? (
                <div className="flex align-items-center gap-3">
                  {thumbnail(x)}
                  <span style={{ fontFamily: "monospace" }}>{x.name}</span>
                </div>
              ) : (
                <span>{props.placeholder}</span>
              )
            }
          />
          <LabelComponent
            text="The picture, its click point and where it was captured are shared. The accuracy is this step's own."
            size="sm"
            color="secondary"
          />
        </div>
      )}
    </Dialog>
  );
}
