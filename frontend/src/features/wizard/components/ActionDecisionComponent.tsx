import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import {
  buildSteps,
  defaultAnswers,
  optionsFor,
  type ActionAnswers,
  type Placement,
} from "@/features/wizard/action-to-steps";
import ActionFieldsComponent from "@/features/wizard/components/ActionFieldsComponent";

/** One answer to "where does it go", worked out from the steps already added. */
export interface PlacementOption {
  id: string;
  label: string;
  iconName: string;
  placement: Placement;
}

interface Props {
  action: RecordedActionDto;

  /** Where this action may land. The first is the default and there is never a zero case. */
  placementOptions: PlacementOption[];

  flowId: number | undefined;
  nextTempId: number;

  onBack: () => void;
  onConfirm: (steps: DraftStepDto[], placement: Placement) => void;
  onSkip: () => void;
}

/**
 * Asks the two questions that turn one recorded action into steps: what it is, and where it
 * goes. Then shows the real form for the first step so it can be corrected before it is added.
 */
export default function ActionDecisionComponent({
  action,
  placementOptions,
  flowId,
  nextTempId,
  onBack,
  onConfirm,
  onSkip,
}: Props) {
  const options = useMemo(() => optionsFor(action), [action]);

  const [optionId, setOptionId] = useState(options[0]?.id ?? "");
  const [placementId, setPlacementId] = useState(placementOptions[0]?.id ?? "");
  const [answers, setAnswers] = useState<ActionAnswers>(() =>
    defaultAnswers(action, options[0]?.id ?? ""),
  );

  const { data: screenshot } = useQuery({
    queryKey: ["recording", "screenshot", action.screenshotIndex],
    queryFn: () => backendApiService.Recording.getScreenshot(action.screenshotIndex!),
    enabled: action.screenshotIndex != null,
    staleTime: Infinity,
  });

  // Nothing added yet means there is nowhere else for it to go.
  const placement: Placement =
    placementOptions.find((x) => x.id === placementId)?.placement ?? {};

  const steps = useMemo(
    () => buildSteps(action, optionId, placement, nextTempId, answers),
    // placement is derived from placementId, so that is the real input.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [action, optionId, placementId, placementOptions, nextTempId, answers],
  );

  // A changed option asks about different things, so it starts from that option's defaults
  // rather than carrying the previous one's answers.
  useEffect(() => setAnswers(defaultAnswers(action, optionId)), [action, optionId]);

  const lead = steps[0];

  const handleCrop = async () => {
    if (!screenshot) return;

    const cropped = await ElectronApiService.imageEditor.openWindow({
      imageBase64: screenshot,
      mode: "EDIT",
    });

    if (typeof cropped === "string")
      setAnswers((previous) => ({ ...previous, template: cropped }));
  };

  const confirm = () => {
    if (lead) onConfirm(steps, placement);
  };

  return (
    <div className="flex flex-column gap-3 pt-3">
      <div className="flex align-items-start gap-3">
        {screenshot && (
          <img
            src={`data:image/png;base64,${screenshot}`}
            alt=""
            className="border-round flex-shrink-0"
            style={{ maxWidth: "14rem", maxHeight: "9rem", objectFit: "contain" }}
          />
        )}

        <div className="flex flex-column gap-1">
          <LabelComponent
            text={action.summary}
            weight="semibold"
          />
          {action.windowTitle && (
            <LabelComponent
              text={`in ${action.windowTitle}`}
              size="xs"
              color="secondary"
            />
          )}
        </div>
      </div>

      <div>
        <LabelComponent
          text="What should this become?"
          size="sm"
          weight="bold"
        />
        <div className="flex flex-column gap-2 mt-2">
          {options.map((option) => (
            <div
              key={option.id}
              onClick={() => setOptionId(option.id)}
              className="flex align-items-center gap-2 p-2 border-round cursor-pointer"
              style={{
                border:
                  optionId === option.id
                    ? "2px solid var(--primary-color)"
                    : "1px solid var(--surface-border)",
              }}
            >
              <IconComponent
                name={option.iconName}
                size="sm"
              />
              <div className="flex flex-column flex-1 min-w-0">
                <LabelComponent
                  text={option.label}
                  size="sm"
                />
                <LabelComponent
                  text={option.description}
                  size="xs"
                  color="secondary"
                />
              </div>
              {option.stepCount > 1 && (
                <Tag
                  value={`${option.stepCount} steps`}
                  severity="info"
                />
              )}
            </div>
          ))}
        </div>
      </div>

      {placementOptions.length > 1 && (
        <div>
          <LabelComponent
            text="Where does it go?"
            size="sm"
            weight="bold"
          />
          <div className="flex flex-column gap-2 mt-2">
            {placementOptions.map((option) => (
              <PlacementChoice
                key={option.id}
                label={option.label}
                iconName={option.iconName}
                isSelected={placementId === option.id}
                onSelect={() => setPlacementId(option.id)}
              />
            ))}
          </div>
        </div>
      )}

      <div className="surface-50 border-round p-3">
        <LabelComponent
          text="Step details"
          size="sm"
          weight="bold"
          className="mb-2"
        />

        <ActionFieldsComponent
          key={`${action.index}-${optionId}`}
          optionId={optionId}
          answers={answers}
          flowId={flowId}
          canCrop={screenshot != null}
          onCrop={() => void handleCrop()}
          onChange={setAnswers}
        />
      </div>

      <div className="flex justify-content-between align-items-center mt-2">
        <Button
          type="button"
          label="Back"
          icon="pi pi-chevron-left"
          onClick={onBack}
          className="p-button-text"
        />

        <div className="flex gap-2">
          <Button
            type="button"
            label="Skip"
            onClick={onSkip}
            className="p-button-text p-button-danger"
          />
          <Button
            type="button"
            label="Add and continue"
            icon="pi pi-chevron-right"
            iconPos="right"
            disabled={!lead}
            onClick={confirm}
          />
        </div>
      </div>
    </div>
  );
}

function PlacementChoice({
  label,
  iconName,
  isSelected,
  onSelect,
}: {
  label: string;
  iconName: string;
  isSelected: boolean;
  onSelect: () => void;
}) {
  return (
    <div
      onClick={onSelect}
      className="flex align-items-center gap-2 p-2 border-round cursor-pointer"
      style={{
        border: isSelected
          ? "2px solid var(--primary-color)"
          : "1px solid var(--surface-border)",
      }}
    >
      <IconComponent
        name={iconName}
        size="sm"
      />
      <LabelComponent
        text={label}
        size="sm"
      />
    </div>
  );
}
