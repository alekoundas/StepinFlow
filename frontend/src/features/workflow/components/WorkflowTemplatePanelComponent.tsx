import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { centreClickOffset } from "@/shared/utils/image-size";
import { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import {
  isImageEditorPoint,
  useWindowImageEditor,
} from "@/windows/image-editor/hooks/use-window-image-editor";
import { useUpdateFlowTemplate } from "@/features/workflow/hooks/use-flow-parts";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { WorkflowUsedByComponent } from "@/features/workflow/components/WorkflowUsedByComponent";

interface Props {
  template: FlowTemplateDto | undefined;
  uses: FlowStepUseDto[] | undefined;
}

/**
 * The Templates tab's right side: the picked picture, edited and saved on its own for every step
 * that links to it. Accuracy and required stay on each step.
 */
export function WorkflowTemplatePanelComponent({ template, uses }: Props) {
  const { openImageEditor } = useWindowImageEditor();
  const updateTemplate = useUpdateFlowTemplate();
  const setTreeRefreshTrigger = useWorkflowStore((x) => x.setTreeRefreshTrigger);

  const [draft, setDraft] = useState<FlowTemplateDto | null>(null);
  const [draftOf, setDraftOf] = useState<number | null>(template?.id ?? null);
  const [error, setError] = useState<string>();

  // A new pick starts from what is saved.
  if ((template?.id ?? null) !== draftOf) {
    setDraftOf(template?.id ?? null);
    setDraft(null);
    setError(undefined);
  }

  if (!template)
    return (
      <LabelComponent
        text="Pick a template to see where it is used."
        color="secondary"
      />
    );

  const shown = draft ?? template;
  const usedBy = (uses ?? []).filter((x) => x.templateNames.includes(template.name));

  // Re-cropping moves every pixel, so the click point goes back to the middle of the new picture.
  const editImage = async () => {
    if (!shown.templateImage) return;

    const edited = await openImageEditor(shown.templateImage);
    if (typeof edited !== "string") return;

    setDraft(new FlowTemplateDto({ ...shown, templateImage: edited, ...centreClickOffset(edited) }));
  };

  const setClickPoint = async () => {
    if (!shown.templateImage) return;

    const point = await openImageEditor(shown.templateImage, "PICK_POINT");
    if (!isImageEditorPoint(point)) return;

    setDraft(new FlowTemplateDto({ ...shown, clickOffsetX: point.x, clickOffsetY: point.y }));
  };

  const save = async () => {
    if (!draft) return;

    setError(undefined);
    try {
      await updateTemplate.mutateAsync(draft);
      setDraft(null);

      // The tree is only hidden, so its rows would keep the old thumbnail.
      setTreeRefreshTrigger({ id: draft.flowId, isFlow: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <div className="flex flex-column gap-3">
      <div>
        <LabelComponent
          text="Template"
          size="xs"
          color="secondary"
        />
        <span
          className="text-lg font-bold"
          style={{ fontFamily: "monospace", overflowWrap: "anywhere" }}
        >
          {template.name}
        </span>
      </div>

      <div
        className="flex align-items-center justify-content-center border-round surface-ground p-3 overflow-hidden"
        style={{ minHeight: "10rem" }}
      >
        {shown.templateImage && (
          <img
            src={`data:image/png;base64,${shown.templateImage}`}
            alt={template.name}
            className="max-w-full"
          />
        )}
      </div>

      <div className="grid m-0 text-sm">
        <span className="col-4 p-1 text-color-secondary">Captured</span>
        <span className="col-8 p-1">
          in a {shown.authoredFlowAreaWidth}×{shown.authoredFlowAreaHeight} area at {shown.authoredDpi} dpi
        </span>
        <span className="col-4 p-1 text-color-secondary">Clicks at</span>
        <span className="col-8 p-1">
          {shown.clickOffsetX}, {shown.clickOffsetY}
        </span>
      </div>

      <div className="flex gap-2 flex-wrap">
        <Button
          type="button"
          label="Edit image"
          icon="pi pi-pencil"
          outlined
          disabled={!shown.templateImage}
          onClick={editImage}
        />
        <Button
          type="button"
          label="Set click point"
          icon="pi pi-crosshairs"
          outlined
          disabled={!shown.templateImage}
          onClick={setClickPoint}
        />
      </div>

      {usedBy.length > 1 && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text={`Shared by ${usedBy.length} steps. A change here reaches all of them; to change one alone, edit a copy from that step.`}
        />
      )}

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      <WorkflowUsedByComponent
        steps={usedBy}
        emptyText="No step searches for it."
      />

      <div className="flex align-items-center justify-content-between gap-2 flex-wrap pt-3 border-top-1 surface-border">
        <LabelComponent
          text="Accuracy and Required are set on each step. A template goes when the last step stops using it."
          size="xs"
          color="secondary"
          className="flex-1"
        />
        <div className="flex gap-2">
          {draft && (
            <Button
              type="button"
              label="Cancel"
              severity="secondary"
              text
              onClick={() => setDraft(null)}
            />
          )}
          <Button
            type="button"
            label="Save"
            disabled={!draft}
            loading={updateTemplate.isPending}
            onClick={save}
          />
        </div>
      </div>
    </div>
  );
}
