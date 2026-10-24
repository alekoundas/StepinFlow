import { classNames } from "primereact/utils";

import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";

interface Props {
  templates: FlowTemplateDto[] | undefined;
  selected: number | null;
  onSelect: (id: number) => void;
}

/** The Templates tab's left side: every picture the flow's steps look for. */
export function WorkflowTemplateGridComponent({ templates, selected, onSelect }: Props) {
  const usedText = (template: FlowTemplateDto) => {
    if (template.flowStepsCount === 1) return "1 step";
    return `${template.flowStepsCount} steps`;
  };

  return (
    <div className="flex flex-column">
      <span className="text-sm text-color-secondary px-3 pt-3 pb-2">
        The pictures steps look for. A step links to one, so a picture can serve every step that
        looks for it.
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
            key={template.id}
            className="col-6 md:col-4 xl:col-3 p-1"
          >
            <button
              type="button"
              aria-pressed={selected === template.id}
              onClick={() => onSelect(template.id)}
              className={classNames(
                "flex flex-column gap-2 w-full p-2 border-1 border-round text-left cursor-pointer",
                selected === template.id ? "border-primary surface-hover" : "surface-border surface-card",
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
              <span className="text-xs text-color-secondary">{usedText(template)}</span>
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
