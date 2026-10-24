import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";

import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";

interface Props {
  template: FlowTemplateDto;
  stepName: string;
  /** The other steps that link to the picture. */
  otherSteps: string[];

  onAll: () => void;
  onCopy: () => void;

  onClose?: () => void; // injected by DialogRootComponent
}

/**
 * Asked before changing a picture other steps link to: change it for all of them, or give this step
 * a copy of its own.
 */
export function FlowTemplateSharedDialogComponent({
  template,
  stepName,
  otherSteps,
  onAll,
  onCopy,
  onClose,
}: Props) {
  const choose = (act: () => void) => {
    onClose?.();
    act();
  };

  const count = otherSteps.length + 1;

  return (
    <Dialog
      visible
      header={`This template is used by ${count} steps`}
      onHide={() => onClose?.()}
      style={{ width: "36rem" }}
      draggable={false}
      footer={
        <div className="flex justify-content-between gap-2 flex-wrap">
          <Button
            type="button"
            label="Cancel"
            text
            severity="secondary"
            onClick={() => onClose?.()}
          />
          <div className="flex gap-2">
            <Button
              type="button"
              label="Edit a copy for this step"
              outlined
              onClick={() => choose(onCopy)}
            />
            <Button
              type="button"
              label={`Edit it for all ${count} steps`}
              onClick={() => choose(onAll)}
            />
          </div>
        </div>
      }
    >
      <div className="flex flex-column gap-3">
        <div className="flex align-items-center gap-3">
          <span
            className="flex align-items-center justify-content-center border-round surface-ground overflow-hidden flex-shrink-0"
            style={{ width: "4.5rem", height: "4.5rem" }}
          >
            {template.templateImage && (
              <img
                src={`data:image/png;base64,${template.templateImage}`}
                alt=""
                className="max-w-full max-h-full"
              />
            )}
          </span>
          <div className="flex flex-column gap-1 min-w-0">
            <span style={{ fontFamily: "monospace" }}>{template.name}</span>
            <LabelComponent
              text={`Also used by ${otherSteps.join(", ")}`}
              size="sm"
              color="secondary"
            />
          </div>
        </div>

        <LabelComponent
          text={`Editing the picture changes what every one of them looks for. To change it for ${stepName || "this step"} alone, edit a copy: this step gets a template of its own, and the others keep this one.`}
          size="sm"
        />
      </div>
    </Dialog>
  );
}
