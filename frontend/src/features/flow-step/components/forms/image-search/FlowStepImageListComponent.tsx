import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { Tag } from "primereact/tag";
import { Checkbox } from "primereact/checkbox";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { ImageSearchTestImageDto } from "@/shared/models/database/image-search-test-result-dto";

interface Props {
  images: FlowStepImageDto[];
  // Keyed by list index, since a freshly added template has no id yet.
  testResults: Map<number, ImageSearchTestImageDto>;
  isDisabled?: boolean;

  onAdd: () => void;
  onEditImage: (index: number) => void;
  onSetClickPoint: (index: number) => void;
  onChange: (index: number, image: FlowStepImageDto) => void;
  onRemove: (index: number) => void;
}

export function FlowStepImageListComponent({
  images,
  testResults,
  isDisabled = false,
  onAdd,
  onEditImage,
  onSetClickPoint,
  onChange,
  onRemove,
}: Props) {
  const anyRequired = images.some((x) => x.isRequired);

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

      <LabelComponent
        size="sm"
        color="secondary"
        text={
          anyRequired
            ? "Every template marked required must be found, or the step fails."
            : "Nothing is marked required, so finding any one of these counts as success."
        }
      />

      {images.length === 0 && (
        <LabelComponent
          size="sm"
          color="secondary"
          text="No templates yet. Capture one to tell the step what to look for."
        />
      )}

      {images.map((image, index) => {
        const result = testResults.get(index);

        return (
          <Card
            key={`${image.id}-${index}`}
            className="shadow-1 border-round-xl"
          >
            <div className="flex gap-3 align-items-center">
              {image.templateImage ? (
                <img
                  src={`data:image/png;base64,${image.templateImage}`}
                  alt={image.name}
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
                  text={image.name || `Template ${index + 1}`}
                  weight="semibold"
                  size="sm"
                />
                <LabelComponent
                  size="xs"
                  color="secondary"
                  text={
                    image.authoredFrameWidth > 0
                      ? `captured in a ${image.authoredFrameWidth}×${image.authoredFrameHeight} frame`
                      : "no frame size recorded, scaling will be skipped"
                  }
                />
              </div>

              {result && (
                <Tag
                  severity={result.isFound ? "success" : "danger"}
                  value={
                    result.isFound
                      ? `found ${result.matchCount} · ${result.bestScore.toFixed(2)}`
                      : "not found"
                  }
                />
              )}

              <div className="flex align-items-center gap-2">
                <Checkbox
                  inputId={`required-${index}`}
                  checked={image.isRequired}
                  disabled={isDisabled}
                  onChange={(e) =>
                    onChange(
                      index,
                      new FlowStepImageDto({ ...image, isRequired: !!e.checked }),
                    )
                  }
                />
                <label htmlFor={`required-${index}`}>
                  <LabelComponent
                    text="Required"
                    size="xs"
                  />
                </label>
              </div>

              {!isDisabled && (
                <ActionsMenuComponent
                  id={index}
                  onEdit={() => onEditImage(index)}
                  onDelete={() => onRemove(index)}
                  extraActions={[
                    {
                      label: "Set click point",
                      icon: "pi pi-crosshairs",
                      command: () => onSetClickPoint(index),
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
