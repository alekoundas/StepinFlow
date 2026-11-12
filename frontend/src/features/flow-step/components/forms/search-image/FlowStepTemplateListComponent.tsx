import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { Tag } from "primereact/tag";
import { Slider } from "primereact/slider";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";
import type { ImageSearchTestImageDto } from "@/shared/models/database/image-search-test-result-dto";
import { MatchRefusalEnum } from "@/shared/enums/backend/match-refusal-enum";
import { refusedOf } from "@/features/flow-step/components/forms/search-image/image-search-test";

interface Props {
  images: FlowStepTemplateDto[];
  // Keyed by list index, since a freshly added template has no id yet.
  testResults: Map<number, ImageSearchTestImageDto>;
  // By list index: the other steps that link to the same picture.
  sharedWith: string[][];
  isDisabled?: boolean;

  onAdd: () => void;
  onEditImage: (index: number) => void;
  onSetClickPoint: (index: number) => void;
  onEditCopy: (index: number) => void;
  onChange: (index: number, image: FlowStepTemplateDto) => void;
  onRemove: (index: number) => void;
}

// What a different screen is measured against: the area's size for one that scales with its
// area, the DPI for one that scales with the monitor.
const capturedIn = (picture: FlowTemplateDto): string => {
  const facts: string[] = [];

  if (picture.authoredFlowAreaWidth > 0)
    facts.push(
      `in a ${picture.authoredFlowAreaWidth}×${picture.authoredFlowAreaHeight} area`,
    );

  if (picture.authoredDpi > 0) facts.push(`at ${picture.authoredDpi} dpi`);

  if (facts.length === 0)
    return "nothing recorded about where it was captured, so it is searched at this size everywhere";

  return `captured ${facts.join(" ")}`;
};

export function FlowStepTemplateListComponent({
  images,
  testResults,
  sharedWith,
  isDisabled = false,
  onAdd,
  onEditImage,
  onSetClickPoint,
  onEditCopy,
  onChange,
  onRemove,
}: Props) {
  // Missing templates never add up to more than one failure: the step fails once, either way.
  let outcome = "";
  if (images.length > 1)
    outcome =
      "Finding any one of these is enough, and they are tried from the top. If none is found, the Failure branch executes once.";
  else if (images.length === 1)
    outcome = "If it is not found, the Failure branch executes.";

  return (
    <div className="flex flex-column gap-2 mt-4">
      <div className="flex justify-content-between align-items-center">
        <LabelComponent
          text="What to look for"
          weight="bold"
        />
        {!isDisabled && (
          <Button
            type="button"
            label="Add template"
            icon="pi pi-plus"
            size="small"
            onClick={onAdd}
          />
        )}
      </div>

      {outcome && (
        <LabelComponent
          size="sm"
          color="secondary"
          text={outcome}
        />
      )}

      {images.length === 0 && (
        <LabelComponent
          size="sm"
          color="secondary"
          text="No templates yet. Capture one, or reuse one of the flow's, to tell the step what to look for."
        />
      )}

      {images.map((image, index) => {
        const result = testResults.get(index);
        const picture = image.flowTemplate;
        const others = sharedWith[index] ?? [];
        const isShared = others.length > 0;

        return (
          <Card
            key={`${image.id}-${index}`}
            className="shadow-1 border-round-xl"
          >
            <div className="flex gap-3 align-items-center">
              {picture.templateImage ? (
                <img
                  src={`data:image/png;base64,${picture.templateImage}`}
                  alt={`Template ${index + 1}`}
                  style={{
                    width: 48,
                    height: 48,
                    objectFit: "contain",
                    imageRendering: "pixelated",
                  }}
                />
              ) : (
                <IconComponent name="image" />
              )}

              <div className="flex flex-column flex-1">
                <LabelComponent
                  text={picture.name || `Template ${index + 1}`}
                  weight="semibold"
                  size="sm"
                />
                <LabelComponent
                  size="xs"
                  color="secondary"
                  text={capturedIn(picture)}
                />
                {isShared && (
                  <span className="flex align-items-center gap-1 text-xs text-primary mt-1">
                    <i className="pi pi-link text-xs" />
                    {`Shared · also used by ${others.join(", ")}`}
                  </span>
                )}

                {/* Per step: one variant of an icon can need a looser bar than another. */}
                <div className="flex align-items-center gap-2 mt-2">
                  <LabelComponent
                    text={`Accuracy ${image.accuracy.toFixed(2)}`}
                    size="xs"
                  />
                  <Slider
                    value={image.accuracy}
                    min={0.1}
                    max={1}
                    step={0.01}
                    disabled={isDisabled}
                    onChange={(e) =>
                      onChange(
                        index,
                        new FlowStepTemplateDto({ ...image, accuracy: e.value as number }),
                      )
                    }
                    style={{ width: "10rem" }}
                  />
                </div>
              </div>

              {result && <ResultTag result={result} />}

              {!isDisabled && (
                <ActionsMenuComponent
                  id={index}
                  extraActions={[
                    {
                      label: "Edit image",
                      icon: "pi pi-pencil",
                      command: () => onEditImage(index),
                    },
                    {
                      label: "Set click point",
                      icon: "pi pi-crosshairs",
                      command: () => onSetClickPoint(index),
                    },
                    ...(isShared
                      ? [
                          {
                            label: "Edit a copy for this step",
                            icon: "pi pi-clone",
                            command: () => onEditCopy(index),
                          },
                        ]
                      : []),
                    {
                      label: "Remove from this step",
                      icon: "pi pi-times",
                      command: () => onRemove(index),
                    },
                  ]}
                />
              )}
            </div>
          </Card>
        );
      })}
    </div>
  );
}

// Found, the right shape refused for its colour, or not found, with the score that says how close.
function ResultTag({ result }: { result: ImageSearchTestImageDto }) {
  const refused = refusedOf(result);

  if (result.isFound)
    return (
      <Tag
        severity="success"
        value={`found ${result.matchCount} · ${result.bestScore.toFixed(2)}`}
      />
    );

  if (refused.length > 0)
    return (
      <Tag
        severity="warning"
        value={`refused for its ${refused[0].refusedBy === MatchRefusalEnum.CONTRAST ? "contrast" : "colour"} · ${refused[0].score.toFixed(2)}`}
      />
    );

  return (
    <Tag
      severity="danger"
      value={`not found · ${result.bestScore.toFixed(2)}`}
    />
  );
}
