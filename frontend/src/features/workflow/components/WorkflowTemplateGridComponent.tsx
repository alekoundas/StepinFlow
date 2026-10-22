import { classNames } from "primereact/utils";

import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";

interface Props {
  templates: FlowStepTemplateDto[] | undefined;
  uses: FlowStepUseDto[] | undefined;
  selected: string | null;
  onSelect: (name: string) => void;
}

/** The Templates tab's left side: every image the flow searches for. */
export function WorkflowTemplateGridComponent({ templates, uses, selected, onSelect }: Props) {
  const usedText = (name: string) => {
    const count = (uses ?? []).filter((x) => x.templateNames.includes(name)).length;
    if (count === 1) return "1 step";
    return `${count} steps`;
  };

  return (
    <div className="flex flex-column">
      <span className="text-sm text-color-secondary px-3 pt-3 pb-2">
        Cut from your recording. Each belongs to the steps that search for it.
      </span>

      {templates && templates.length === 0 && (
        <LabelComponent
          text="No step searches for an image yet."
          color="secondary"
          className="px-3"
        />
      )}

      <div className="grid m-0 px-2">
        {(templates ?? []).map((template) => (
          <div
            key={template.name}
            className="col-6 md:col-4 xl:col-3 p-1"
          >
            <button
              type="button"
              aria-pressed={selected === template.name}
              onClick={() => onSelect(template.name)}
              className={classNames(
                "flex flex-column gap-2 w-full p-2 border-1 border-round text-left cursor-pointer",
                selected === template.name ? "border-primary surface-hover" : "surface-border surface-card",
              )}
              style={{ font: "inherit", color: "inherit" }}
            >
              <span
                className="flex align-items-center justify-content-center border-round surface-ground overflow-hidden"
                style={{ height: "4.5rem" }}
              >
                {template.templateImage && (
                  <img
                    src={`data:image/png;base64,${template.templateImage}`}
                    alt=""
                    className="max-w-full max-h-full"
                  />
                )}
              </span>
              <span
                className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis"
                style={{ fontFamily: "monospace" }}
              >
                {template.name}
              </span>
              <span className="text-xs text-color-secondary">{usedText(template.name)}</span>
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
