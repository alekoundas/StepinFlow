import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { hasBranches } from "@/shared/models/flow-step-catalog";
import { useFlowStep } from "@/features/flow-step/hooks/use-flow-step";
import { useExecutionStepScreenshot } from "@/features/execution/hooks/use-execution";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  executionStep: ExecutionStepDto | undefined;

  /** Every step of the run, so this one can name the step it ran inside. */
  executionSteps: ExecutionStepDto[];
}

/**
 * What one step produced. Only the fields it actually filled in - a cursor step has a location and
 * no value, a command has an exit code and no location, and showing the empty ones is noise.
 */
export default function ExecutionStepDetailComponent({
  executionStep,
  executionSteps,
}: Props) {
  if (!executionStep)
    return (
      <div className="p-4">
        <LabelComponent
          text="Select a step to see what it produced."
          size="sm"
          color="secondary"
        />
      </div>
    );

  const parent = executionSteps.find(
    (x) => x.sequence === executionStep.parentSequence,
  );

  const isFailure = executionStep.outcome === StepOutcomeEnum.FAILURE;

  return (
    <div className="flex flex-column gap-3 p-3">
      <div className="flex align-items-center gap-2">
        <LabelComponent
          text={executionStep.name}
          weight="semibold"
          color={isFailure ? "error" : undefined}
        />

        {hasBranches(executionStep.flowStepType) ? (
          <Tag
            severity={isFailure ? "danger" : "success"}
            value={isFailure ? "Failed" : "Succeeded"}
          />
        ) : null}
      </div>

      <div className="flex flex-column gap-2">
        <DetailRow
          label="Type"
          value={executionStep.flowStepType}
        />
        <DetailRow
          label="Sequence"
          value={executionStep.sequence}
        />
        <DetailRow
          label="Depth"
          value={executionStep.depth}
        />

        {parent ? (
          <DetailRow
            label="Inside"
            value={`#${parent.sequence} ${parent.name}`}
          />
        ) : null}

        <DetailRow
          label="Took"
          value={`${executionStep.durationMilliseconds} ms`}
        />

        {executionStep.resultLocationX !== null &&
        executionStep.resultLocationX !== undefined ? (
          <DetailRow
            label="Location"
            value={`${executionStep.resultLocationX}, ${executionStep.resultLocationY}`}
          />
        ) : null}

        {executionStep.matchCount ? (
          <DetailRow
            label="Match"
            value={`${(executionStep.matchIndex ?? 0) + 1} of ${executionStep.matchCount}`}
          />
        ) : null}

        {executionStep.exitCode !== null && executionStep.exitCode !== undefined ? (
          <DetailRow
            label="Exit code"
            value={executionStep.exitCode}
          />
        ) : null}
      </div>

      {executionStep.flowStepType === FlowStepTypeEnum.IMAGE_SEARCH &&
      executionStep.flowStepId ? (
        <TemplateImages flowStepId={executionStep.flowStepId} />
      ) : null}

      {executionStep.flowStepType === FlowStepTypeEnum.IMAGE_SEARCH ? (
        <StepScreenshot
          executionStepId={executionStep.id}
          fileName={executionStep.screenshotFileName}
        />
      ) : null}

      {executionStep.value ? (
        <div className="flex flex-column gap-1">
          <LabelComponent
            text="Produced"
            size="sm"
            color="secondary"
          />
          <div className="surface-ground border-1 surface-border border-round p-2 text-sm">
            {executionStep.value}
          </div>
        </div>
      ) : null}

      {executionStep.message ? (
        <div className="flex flex-column gap-1">
          <LabelComponent
            text="What happened"
            size="sm"
            color="secondary"
          />
          <div className="surface-ground border-1 surface-border border-round p-2 text-sm">
            {executionStep.message}
          </div>
        </div>
      ) : null}

      {executionStep.error ? (
        <div className="flex flex-column gap-1">
          <LabelComponent
            text="Standard error"
            size="sm"
            color="secondary"
          />
          <div className="surface-ground border-1 surface-border border-round p-2 text-sm">
            {executionStep.error}
          </div>
        </div>
      ) : null}
    </div>
  );
}

interface TemplateImagesProps {
  flowStepId: number;
}

function TemplateImages({ flowStepId }: TemplateImagesProps) {
  const { data: flowStep } = useFlowStep(flowStepId);

  const images = flowStep?.flowStepImages ?? [];
  if (images.length === 0) return null;

  return (
    <div className="flex flex-column gap-1">
      <LabelComponent
        text="Template images"
        size="sm"
        color="secondary"
      />

      <div className="flex flex-wrap gap-2">
        {images.map((image) => (
          <div
            key={image.id}
            className="surface-ground border-1 surface-border border-round p-2 flex flex-column align-items-center gap-1"
            style={{ width: 96 }}
          >
            {image.templateImage ? (
              <img
                src={`data:image/png;base64,${image.templateImage}`}
                alt={image.name}
                style={{
                  width: 64,
                  height: 64,
                  objectFit: "contain",
                  imageRendering: "pixelated",
                }}
              />
            ) : (
              <IconComponent name="image" />
            )}

            <LabelComponent
              text={image.name || `Template ${image.orderNumber + 1}`}
              size="xs"
            />

            {image.isRequired && (
              <Tag
                severity="info"
                value="required"
              />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

interface StepScreenshotProps {
  executionStepId: number;

  /** Whether the run kept one at all, which the fetch alone cannot tell from a deleted file. */
  fileName?: string | null;
}

function StepScreenshot({ executionStepId, fileName }: StepScreenshotProps) {
  const { data: screenshot } = useExecutionStepScreenshot(
    fileName ? executionStepId : null,
  );

  return (
    <div className="flex flex-column gap-1">
      <LabelComponent
        text="Screenshot"
        size="sm"
        color="secondary"
      />

      {screenshot ? (
        <img
          src={`data:image/jpeg;base64,${screenshot}`}
          alt=""
          className="w-full h-auto border-round-sm block"
        />
      ) : (
        <LabelComponent
          text={
            fileName
              ? "That screenshot is no longer on disk."
              : "Not kept. Run with screenshots on, and raise the per-run limit in Settings."
          }
          size="xs"
          color="secondary"
        />
      )}
    </div>
  );
}

interface DetailRowProps {
  label: string;
  value: string | number;
}

function DetailRow({ label, value }: DetailRowProps) {
  return (
    <div className="flex justify-content-between gap-3">
      <LabelComponent
        text={label}
        size="sm"
        color="secondary"
      />
      <LabelComponent
        text={value}
        size="sm"
      />
    </div>
  );
}
