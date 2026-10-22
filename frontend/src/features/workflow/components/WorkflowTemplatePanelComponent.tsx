import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import { WorkflowUsedByComponent } from "@/features/workflow/components/WorkflowUsedByComponent";

interface Props {
  template: FlowStepTemplateDto | undefined;
  uses: FlowStepUseDto[] | undefined;
}

/**
 * The Templates tab's right side: the picked template and the steps that search for it. Read
 * only, since a template is changed from a step that uses it.
 */
export function WorkflowTemplatePanelComponent({ template, uses }: Props) {
  if (!template)
    return (
      <LabelComponent
        text="Pick a template to see where it is used."
        color="secondary"
      />
    );

  const usedBy = (uses ?? []).filter((x) => x.templateNames.includes(template.name));

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
        {template.templateImage && (
          <img
            src={`data:image/png;base64,${template.templateImage}`}
            alt={template.name}
            className="max-w-full"
          />
        )}
      </div>

      <div className="grid m-0 text-sm">
        <span className="col-4 p-1 text-color-secondary">Captured</span>
        <span className="col-8 p-1">
          {template.authoredFlowAreaWidth}×{template.authoredFlowAreaHeight} at {template.authoredDpi} dpi
        </span>
        <span className="col-4 p-1 text-color-secondary">Clicks at</span>
        <span className="col-8 p-1">
          {template.clickOffsetX}, {template.clickOffsetY}
        </span>
        <span className="col-4 p-1 text-color-secondary">Accuracy</span>
        <span className="col-8 p-1">{template.accuracy.toFixed(2)}</span>
      </div>

      <WorkflowUsedByComponent
        steps={usedBy}
        emptyText="No step searches for it."
      />

      <LabelComponent
        text="Change or replace it from a step that searches for it."
        size="xs"
        color="secondary"
        className="pt-3 border-top-1 surface-border"
      />
    </div>
  );
}
