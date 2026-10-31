import { Link, useNavigate } from "react-router-dom";
import { Tag } from "primereact/tag";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import StatusPillComponent, {
  type StatusPillSeverity,
} from "@/shared/components/StatusPillComponent";
import ExecutionPanelComponent from "@/features/execution/components/templates/ExecutionPanelComponent";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { getFlowStepCatalogEntry, hasBranches } from "@/shared/models/flow-step-catalog";
import { DONE_COLOR, HANDLED_COLOR, INSET, MONO, MUTED } from "@/features/execution/execution-format";
import { findStep, isCheckStep } from "@/features/execution/hooks/use-execution-overview";
import { useFlowStep } from "@/features/flow-step/hooks/use-flow-step";
import { useExecutionStepScreenshot } from "@/features/execution/hooks/use-execution";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { useAiStatus } from "@/features/ai/hooks/use-ai";
import { useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  executionStep: ExecutionStepDto | undefined;

  /** Every step of the execution, so this one can name the step it executed inside. */
  executionSteps: ExecutionStepDto[];

  fatalSequence: number | undefined;

  /** What the execution was set to keep, which is why a step has no screenshot. */
  historyLevel: ExecutionHistoryLevelEnum | undefined;

  onSelect: (sequence: number) => void;
}

/**
 * What one step produced. Only the fields it actually filled in - a cursor step has a location and
 * no value, a command has an exit code and no location, and showing the empty ones is noise.
 */
export default function ExecutionStepDetailComponent({
  executionStep,
  executionSteps,
  fatalSequence,
  historyLevel,
  onSelect,
}: Props) {
  if (!executionStep)
    return (
      <ExecutionPanelComponent title="Step">
        <div className="p-4">
          <LabelComponent
            text="Select a step to see what it produced."
            size="sm"
            color="secondary"
          />
        </div>
      </ExecutionPanelComponent>
    );

  const outcome = outcomeOf(executionStep, executionStep.sequence === fatalSequence);

  return (
    <ExecutionPanelComponent
      title={
        <>
          Step{" "}
          <span
            className="font-normal"
            style={{ ...MUTED, fontFamily: MONO }}
          >
            {executionStep.sequence.toLocaleString()}
          </span>
        </>
      }
      actions={
        <StatusPillComponent
          text={outcome.text}
          severity={outcome.severity}
        />
      }
    >
      <StepDetail
        executionStep={executionStep}
        executionSteps={executionSteps}
        historyLevel={historyLevel}
        onSelect={onSelect}
      />
    </ExecutionPanelComponent>
  );
}

interface StepDetailProps {
  executionStep: ExecutionStepDto;
  executionSteps: ExecutionStepDto[];
  historyLevel: ExecutionHistoryLevelEnum | undefined;
  onSelect: (sequence: number) => void;
}

function StepDetail({ executionStep, executionSteps, historyLevel, onSelect }: StepDetailProps) {
  const parent =
    executionStep.parentSequence !== null && executionStep.parentSequence !== undefined
      ? findStep(executionSteps, executionStep.parentSequence)
      : undefined;

  const isFailure = executionStep.outcome === StepOutcomeEnum.FAILURE;
  const isSearchImage = executionStep.flowStepType === FlowStepTypeEnum.SEARCH_IMAGE;

  return (
    <div className="flex flex-column gap-3 p-3 flex-1">
      <div className="flex flex-column gap-1">
        <span className="text-base font-semibold">{executionStep.name}</span>
        {parent ? (
          <button
            type="button"
            className="p-0 border-none bg-transparent cursor-pointer text-left text-xs"
            style={{ ...MUTED, font: "inherit", fontSize: "0.75rem" }}
            title="Select the step it executed inside"
            onClick={() => onSelect(parent.sequence)}
          >
            {whereOf(parent)}
          </button>
        ) : (
          <span
            className="text-xs"
            style={MUTED}
          >
            At the top of the flow
          </span>
        )}
      </div>

      <dl
        className="m-0"
        style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 8 }}
      >
        {factsOf(executionStep).map((fact) => (
          <div
            key={fact.label}
            className="px-2 py-1 border-1 border-round-lg min-w-0"
            style={INSET}
          >
            <dt
              className="text-xs"
              style={MUTED}
            >
              {fact.label}
            </dt>
            <dd
              className="m-0 text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis"
              style={{ fontFamily: MONO }}
              title={fact.value}
            >
              {fact.value}
            </dd>
          </div>
        ))}
      </dl>

      {isSearchImage &&
      executionStep.flowStepId &&
      executionStep.bestScore !== null &&
      executionStep.bestScore !== undefined ? (
        <AccuracyMeterComponent
          flowStepId={executionStep.flowStepId}
          bestTemplateId={executionStep.bestTemplateId}
          bestScore={executionStep.bestScore}
        />
      ) : null}

      {isSearchImage && executionStep.flowStepId ? (
        <TemplateImages flowStepId={executionStep.flowStepId} />
      ) : null}

      {isSearchImage ? (
        <StepScreenshot
          executionStep={executionStep}
          historyLevel={historyLevel}
        />
      ) : null}

      {executionStep.value ? (
        <TextBlock
          label="Produced"
          text={executionStep.value}
        />
      ) : null}

      {executionStep.message ? (
        <TextBlock
          label="What happened"
          text={executionStep.message}
        />
      ) : null}

      {executionStep.error ? (
        <TextBlock
          label="Standard error"
          text={executionStep.error}
        />
      ) : null}

      <div className="flex flex-wrap gap-2 mt-auto pt-2">
        <OpenInEditorButton flowStepId={executionStep.flowStepId} />
        {isFailure ? <SuggestFixButton executionStep={executionStep} /> : null}
      </div>
    </div>
  );
}

interface Outcome {
  text: string;
  severity: StatusPillSeverity;
}

function outcomeOf(executionStep: ExecutionStepDto, isFatal: boolean): Outcome {
  if (isFatal) return { text: "Ended the execution", severity: "danger" };
  if (executionStep.flowStepType === FlowStepTypeEnum.LOOP)
    return { text: "Came round", severity: "neutral" };

  if (executionStep.outcome === StepOutcomeEnum.FAILURE)
    return { text: hasBranches(executionStep.flowStepType) ? "Handled" : "Failed", severity: "warning" };

  if (!isCheckStep(executionStep)) return { text: "Done", severity: "success" };

  const isSearch =
    executionStep.flowStepType === FlowStepTypeEnum.SEARCH_IMAGE ||
    executionStep.flowStepType === FlowStepTypeEnum.SEARCH_TEXT;
  return { text: isSearch ? "Found" : "Succeeded", severity: "success" };
}

/** Where a step sat, said by the step it executed inside. */
function whereOf(parent: ExecutionStepDto): string {
  if (parent.flowStepType === FlowStepTypeEnum.LOOP)
    return `${parent.name}, pass ${((parent.loopPass ?? 0) + 1).toLocaleString()}`;

  if (hasBranches(parent.flowStepType))
    return `${parent.outcome === StepOutcomeEnum.SUCCESS ? "Success" : "Failure"} of ${parent.name}`;

  return `Inside ${parent.name}`;
}

interface Fact {
  label: string;
  value: string;
}

function factsOf(executionStep: ExecutionStepDto): Fact[] {
  const facts: Fact[] = [
    {
      label: "Type",
      value: getFlowStepCatalogEntry(executionStep.flowStepType)?.label ?? executionStep.flowStepType,
    },
    { label: "Took", value: `${executionStep.durationMilliseconds} ms` },
  ];

  if (executionStep.resultLocationX !== null && executionStep.resultLocationX !== undefined)
    facts.push({
      label: "Location",
      value: `${executionStep.resultLocationX}, ${executionStep.resultLocationY}`,
    });

  if (executionStep.matchCount)
    facts.push({
      label: "Hit",
      value: `${(executionStep.matchIndex ?? 0) + 1} of ${executionStep.matchCount}`,
    });

  if (executionStep.loopPass !== null && executionStep.loopPass !== undefined)
    facts.push({ label: "Pass", value: (executionStep.loopPass + 1).toLocaleString() });

  if (executionStep.exitCode !== null && executionStep.exitCode !== undefined)
    facts.push({ label: "Exit code", value: String(executionStep.exitCode) });

  return facts;
}

interface TextBlockProps {
  label: string;
  text: string;
}

function TextBlock({ label, text }: TextBlockProps) {
  return (
    <div className="flex flex-column gap-1">
      <span
        className="text-xs"
        style={MUTED}
      >
        {label}
      </span>
      <div
        className="border-1 border-round-lg p-2 text-sm"
        style={{ ...INSET, overflowWrap: "anywhere" }}
      >
        {text}
      </div>
    </div>
  );
}

interface OpenInEditorButtonProps {
  flowStepId?: number | null;
}

/** The step as it is now, in the flow it belongs to - a sub-flow's step opens the sub-flow. */
function OpenInEditorButton({ flowStepId }: OpenInEditorButtonProps) {
  const navigate = useNavigate();
  const setStepToReveal = useWorkflowStore((x) => x.setStepToReveal);
  const { data: flowStep } = useFlowStep(flowStepId ?? null);

  const flowId = flowStep?.flowId;

  return (
    <Button
      label="Open in the editor"
      icon="pi pi-pencil"
      size="small"
      outlined
      disabled={!flowStepId || !flowId}
      tooltip={flowStepId ? undefined : "The step has been deleted since."}
      tooltipOptions={{ showOnDisabled: true }}
      onClick={() => {
        if (!flowStepId || !flowId) return;

        setStepToReveal(flowStepId);
        navigate(`/workflow/${flowId}`);
      }}
    />
  );
}

interface SuggestFixButtonProps {
  executionStep: ExecutionStepDto;
}

/**
 * Asks the chat about this step, with the ids already in the question.
 *
 * Not its own one-shot answer: a fix is a conversation. "Lower the accuracy to 0.7" invites "why?"
 * and "what else could it be?", and the chat can take those where a single verdict cannot.
 */
function SuggestFixButton({ executionStep }: SuggestFixButtonProps) {
  const { data: isConfigured } = useAiStatus();
  const { open, startConversation } = useAiChatStore();
  const { ask } = useAskAi();

  if (!isConfigured || !executionStep.flowStepId) return null;

  const suggest = () => {
    const conversationId = startConversation();
    open();

    ask(
      conversationId,
      `Step ${executionStep.flowStepId} ("${executionStep.name}") failed in execution ${executionStep.executionId}. Why did it fail, and how do I fix it?`,
      executionStep.executionId,
    );
  };

  return (
    <Button
      label="Suggest a fix"
      icon="pi pi-sparkles"
      size="small"
      onClick={suggest}
    />
  );
}

interface AccuracyMeterProps {
  flowStepId: number;
  bestTemplateId?: number | null;
  bestScore: number;
}

/**
 * What the search needed against what it got.
 *
 * A failed image search otherwise says only that it failed, and the three usual causes - the
 * accuracy being a shade tight, the search area, a template captured at another window size - read
 * identically until you can see how close it came.
 *
 * Each template has its own accuracy, so the line is the one belonging to the template that came
 * closest - as it stands now, not a copy taken during the execution, so editing that template moves
 * it.
 */
function AccuracyMeterComponent({ flowStepId, bestTemplateId, bestScore }: AccuracyMeterProps) {
  const { data: flowStep } = useFlowStep(flowStepId);

  const template = flowStep?.flowStepTemplates.find((x) => x.id === bestTemplateId);
  if (!template) return null;

  const accuracy = template.accuracy;
  const isPass = bestScore >= accuracy;
  const color = isPass ? DONE_COLOR : HANDLED_COLOR;

  return (
    <div className="flex flex-column gap-2">
      <div className="flex justify-content-between gap-2 text-sm white-space-nowrap">
        <span>
          Match{" "}
          <span style={{ fontFamily: MONO }}>{bestScore.toFixed(2)}</span>
          {template.flowTemplate?.name ? (
            <span style={MUTED}> · {template.flowTemplate.name}</span>
          ) : null}
        </span>
        <span style={MUTED}>
          needs <span style={{ fontFamily: MONO }}>{accuracy.toFixed(2)}</span>
        </span>
      </div>

      <div
        role="img"
        aria-label={`Best match ${bestScore.toFixed(2)} against ${accuracy.toFixed(2)} needed`}
        style={{
          position: "relative",
          height: 6,
          borderRadius: 999,
          background: "var(--surface-ground)",
        }}
      >
        <div
          style={{
            position: "absolute",
            left: 0,
            top: 0,
            bottom: 0,
            width: `${Math.min(1, Math.max(0, bestScore)) * 100}%`,
            borderRadius: 999,
            background: color,
          }}
        />
        {/* Where the bar had to reach. A number on its own does not say how near it came. */}
        <div
          style={{
            position: "absolute",
            left: `${Math.min(1, Math.max(0, accuracy)) * 100}%`,
            top: -3,
            width: 2,
            height: 12,
            background: "var(--text-color-secondary)",
          }}
        />
      </div>

      <span
        className="text-xs"
        style={{ color }}
      >
        {isPass
          ? "Cleared it"
          : `Short by ${(accuracy - bestScore).toFixed(2)}, so its Failure branch took over`}
      </span>
    </div>
  );
}

interface TemplateImagesProps {
  flowStepId: number;
}

function TemplateImages({ flowStepId }: TemplateImagesProps) {
  const { data: flowStep } = useFlowStep(flowStepId);

  const images = flowStep?.flowStepTemplates ?? [];
  if (images.length === 0) return null;

  return (
    <div className="flex flex-column gap-1">
      <span
        className="text-xs"
        style={MUTED}
      >
        {images.length === 1 ? "Template image" : "Template images"}
      </span>

      <div className="flex flex-wrap gap-2">
        {images.map((image) => (
          <div
            key={image.id}
            className="border-1 border-round-lg p-2 flex flex-column align-items-center gap-1"
            style={{ ...INSET, width: 96 }}
          >
            {image.flowTemplate?.templateImage ? (
              <img
                src={`data:image/png;base64,${image.flowTemplate.templateImage}`}
                alt={image.flowTemplate.name}
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
              text={image.flowTemplate?.name || `Template ${image.orderNumber + 1}`}
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
  executionStep: ExecutionStepDto;
  historyLevel: ExecutionHistoryLevelEnum | undefined;
}

function StepScreenshot({ executionStep, historyLevel }: StepScreenshotProps) {
  const fileName = executionStep.screenshotFileName;
  const { data: screenshot } = useExecutionStepScreenshot(fileName ? executionStep.id : null);

  if (screenshot)
    return (
      <div className="flex flex-column gap-1">
        <span
          className="text-xs"
          style={MUTED}
        >
          Screenshot
        </span>
        <img
          src={`data:image/jpeg;base64,${screenshot}`}
          alt=""
          className="w-full h-auto border-round-lg block"
        />
      </div>
    );

  return (
    <div
      className="flex gap-2 align-items-start px-3 py-2 border-1 border-round-lg text-xs"
      style={{ ...MUTED, borderColor: "var(--surface-border)" }}
    >
      <i
        className="pi pi-image"
        style={{ fontSize: "0.85rem", marginTop: 1 }}
      />
      <span>
        <ScreenshotNote
          executionStep={executionStep}
          historyLevel={historyLevel}
        />
      </span>
    </div>
  );
}

function ScreenshotNote({ executionStep, historyLevel }: StepScreenshotProps) {
  const { data: settings = [] } = useAppSettings();
  const limit = settings.find((x) => x.key === AppSettingKeyEnum.EXECUTION_SCREENSHOT_LIMIT)?.value;

  // A live row is the broadcast's, which carries no file; the saved rows replace it at the end.
  if (executionStep.id === 0) return <>A kept screenshot shows here once the execution has ended.</>;
  if (executionStep.screenshotFileName) return <>That screenshot is no longer on disk.</>;
  if (historyLevel && historyLevel !== ExecutionHistoryLevelEnum.STEPS_AND_IMAGES)
    return <>No screenshot. This execution kept steps only.</>;

  return (
    <>
      No screenshot. An execution keeps its first {limit ?? "few"}, and they went on earlier steps;
      the limit is under <Link to="/settings/execution">Settings, Execution</Link>.
    </>
  );
}
