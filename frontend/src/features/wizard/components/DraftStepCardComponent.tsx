import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { Tag } from "primereact/tag";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import { getFlowStepCatalogEntry } from "@/shared/models/flow-step-catalog";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

interface Props {
  step: DraftStepDto;
  index: number;
  isSelected: boolean;
  onSelect: () => void;
  /** Receives the cropped template, so the page never has to know about the editor window. */
  onPromoteToImageSearch: (templateBase64: string) => void;
  onRemove: () => void;
}

/**
 * One proposed step: what it is, why it was proposed, and what is still missing.
 *
 * The screenshot is fetched per card rather than shipped with the draft, because most steps
 * never had one and a session holds dozens of them.
 */
export default function DraftStepCardComponent({
  step,
  index,
  isSelected,
  onSelect,
  onPromoteToImageSearch,
  onRemove,
}: Props) {
  const screenshotIndex = step.evidence?.screenshotIndex ?? null;

  const { data: screenshot } = useQuery({
    queryKey: ["recording", "screenshot", screenshotIndex],
    queryFn: () => backendApiService.Recording.getScreenshot(screenshotIndex!),
    enabled: screenshotIndex != null,
    staleTime: Infinity,
  });

  const entry = getFlowStepCatalogEntry(step.values.flowStepType);
  const errors = step.unresolved.filter((x) => x.severity === ValidationSeverityEnum.ERROR);
  const warnings = step.unresolved.filter((x) => x.severity !== ValidationSeverityEnum.ERROR);

  // A recorded click lands on fixed coordinates, which stop being right the moment the window
  // moves. The screenshot is already here, so trading it for an image search is one press.
  const canPromote =
    screenshot != null && step.values.flowStepType === FlowStepTypeEnum.CURSOR_CLICK;

  // The capture is a region around the pointer, so it holds the button plus whatever sat next
  // to it. Cropping is what turns that into a template worth matching.
  const handlePromote = async () => {
    if (!screenshot) return;

    const cropped = await ElectronApiService.imageEditor.openWindow({
      imageBase64: screenshot,
      mode: "EDIT",
    });

    if (typeof cropped === "string") onPromoteToImageSearch(cropped);
  };

  return (
    <Card
      className={isSelected ? "border-primary border-2" : undefined}
      onClick={onSelect}
    >
      <div className="flex align-items-start gap-3">
        {screenshot ? (
          <img
            src={`data:image/png;base64,${screenshot}`}
            alt=""
            className="border-round-sm flex-shrink-0"
            style={{ width: "8rem", maxHeight: "6rem", objectFit: "cover" }}
          />
        ) : (
          <span className="flex align-items-center justify-content-center flex-shrink-0 w-3rem h-3rem border-round-sm surface-100">
            <IconComponent name={entry?.iconName ?? "circle"} />
          </span>
        )}

        <div className="flex flex-column gap-1 flex-1 min-w-0">
          <div className="flex align-items-center gap-2">
            <LabelComponent
              text={`${index + 1}. ${step.values.name}`}
              weight="semibold"
            />
            <Tag
              value={entry?.label ?? step.values.flowStepType}
              severity={errors.length > 0 ? "danger" : "info"}
            />
          </div>

          {step.evidence?.summary && (
            <LabelComponent
              text={step.evidence.summary}
              size="sm"
              color="secondary"
            />
          )}

          {step.evidence?.windowTitle && (
            <LabelComponent
              text={`in ${step.evidence.windowTitle}`}
              size="xs"
              color="secondary"
            />
          )}

          {[...errors, ...warnings].map((issue) => (
            <LabelComponent
              key={`${issue.code}-${issue.message}`}
              text={issue.message}
              size="xs"
              color={issue.severity === ValidationSeverityEnum.ERROR ? "error" : undefined}
            />
          ))}
        </div>

        <div className="flex flex-column gap-2 flex-shrink-0">
          {canPromote && (
            <Button
              type="button"
              label="Find by image"
              icon="pi pi-search"
              onClick={(e) => {
                e.stopPropagation();
                void handlePromote();
              }}
              className="p-button-sm"
              tooltip="Click what this image shows instead of fixed coordinates, so the step survives the window moving"
              tooltipOptions={{ position: "left" }}
            />
          )}

          <Button
            type="button"
            icon="pi pi-trash"
            onClick={(e) => {
              e.stopPropagation();
              onRemove();
            }}
            className="p-button-sm p-button-text p-button-danger"
            tooltip="Drop this step"
            tooltipOptions={{ position: "left" }}
          />
        </div>
      </div>
    </Card>
  );
}
