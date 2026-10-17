import { useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import { FlowGenerationStageEnum } from "@/shared/enums/backend/flow-generation-stage-enum";
import { FlowGenerationStateEnum } from "@/shared/enums/backend/flow-generation-state-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import type { FlowGenerationProgressDto } from "@/shared/models/flow-generation-progress-dto";
import type { FlowGenerationResultDto } from "@/shared/models/flow-generation-result-dto";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";
import type { RecordingResultDto } from "@/shared/models/recording-result-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { type ActionReview, listedActions } from "@/features/wizard/review/review";

// The theme's own variables: PrimeFlex 4's surface and text-color classes read tokens this theme
// does not define.
const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };
const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

type StageStatus = "done" | "active" | "pending";

interface Stage {
  title: string;
  detail: string;
  refused?: FlowScriptErrorDto[];
  refusedNote?: string;
}

interface Props {
  flowId: number;
  recording: RecordingResultDto;
  reviews: Record<number, ActionReview>;
  progress: FlowGenerationProgressDto;
  result: FlowGenerationResultDto | null;
  onCancel: () => void;
  onBack: () => void;
  onCheck: () => void;
}

/**
 * The model shaping the reviewed recording, stage by stage, and what it was sent beside it. The
 * first three stages are the stop's, done before Review. A refused answer shows why, as it goes
 * back to the model. Nothing is saved until an answer reads cleanly, and when none does, the plain
 * draft is, and this says why. What was saved is read on the Check page.
 */
export default function RecordingGeneratingComponent({
  flowId,
  recording,
  reviews,
  progress,
  result,
  onCancel,
  onBack,
  onCheck,
}: Props) {
  const isDone = progress.state === FlowGenerationStateEnum.DONE;
  const stages = stagesOf(recording, reviews, progress, result);
  const active = activeIndexOf(progress);

  let heading = "Generating the flow";
  if (isDone && result?.isModelDraft) heading = "The flow is ready to check";
  if (isDone && result && !result.isModelDraft) heading = "The plain draft was saved";
  if (isDone && !result) heading = "Nothing was saved";

  return (
    <div className="flex flex-wrap align-items-start gap-4">
      <section
        aria-labelledby="generating-heading"
        className="flex flex-column gap-4"
        style={{ flex: "999 1 520px", minWidth: 0 }}
      >
        <div className="flex flex-column gap-1">
          <h2
            id="generating-heading"
            className="m-0 text-2xl font-semibold"
            style={{ letterSpacing: "-0.02em" }}
          >
            {heading}
          </h2>
          <p
            className="m-0"
            style={MUTED}
          >
            Nothing is saved until the script reads cleanly. A refused attempt leaves the flow exactly as it was.
          </p>
        </div>

        {progress.errorMessage && (
          <Message
            severity="error"
            className="w-full justify-content-start"
            text={progress.errorMessage}
          />
        )}

        <ol
          aria-label="Stages"
          className="list-none m-0 p-2 border-1 border-round-xl flex flex-column gap-1"
          style={CARD}
        >
          {stages.map((stage, index) => (
            <StageComponent
              key={stage.title}
              stage={stage}
              status={statusOf(index, active)}
            />
          ))}
        </ol>

        <div className="flex align-items-center justify-content-between flex-wrap gap-3">
          <Button
            type="button"
            label={isDone ? "Back to the recording" : "Cancel"}
            onClick={isDone ? onBack : onCancel}
            severity="secondary"
            text
          />
          {isDone && result?.import.isSuccess && (
            <Button
              type="button"
              label="Check the flow"
              icon="pi pi-arrow-right"
              iconPos="right"
              onClick={onCheck}
            />
          )}
        </div>
      </section>

      <aside style={{ flex: "1 1 300px", minWidth: 0 }}>
        <SentComponent
          flowId={flowId}
          recording={recording}
          reviews={reviews}
          progress={progress}
          request={result?.request ?? ""}
        />
      </aside>
    </div>
  );
}

function StageComponent({ stage, status }: { stage: Stage; status: StageStatus }) {
  const isDone = status === "done";
  const isActive = status === "active";

  let dot: CSSProperties = { border: "1px solid var(--surface-border)" };
  if (isDone) dot = { background: "color-mix(in srgb, var(--primary-color) 15%, transparent)", color: "var(--primary-color)" };
  if (isActive) dot = { border: "1px solid var(--primary-color)" };

  return (
    <li
      aria-current={isActive ? "step" : undefined}
      className="flex gap-3 p-3 border-round-lg"
      style={{ background: isActive ? "var(--surface-hover)" : "transparent" }}
    >
      <span
        className="flex align-items-center justify-content-center border-circle flex-none"
        style={{ width: 24, height: 24, ...dot }}
      >
        {isDone && (
          <i
            className="pi pi-check text-xs"
            aria-hidden="true"
          />
        )}
        {isActive && (
          <span
            className="border-circle"
            style={{ width: 8, height: 8, background: "var(--primary-color)" }}
          />
        )}
      </span>
      <span className="flex flex-column gap-1 min-w-0">
        <span
          className="font-medium"
          style={{ color: isDone || isActive ? "var(--text-color)" : "var(--text-color-secondary)" }}
        >
          {stage.title}
        </span>
        <span
          className="text-sm"
          style={MUTED}
        >
          {stage.detail}
        </span>
        {stage.refused && stage.refused.length > 0 && (
          <RefusedComponent
            refused={stage.refused}
            note={stage.refusedNote}
          />
        )}
      </span>
    </li>
  );
}

// Each line as the scanner said it, which is also what went back to the model.
export function RefusedComponent({ refused, note }: { refused: FlowScriptErrorDto[]; note?: string }) {
  return (
    <span
      className="block mt-2 px-3 py-2 border-1 border-round-lg text-xs"
      style={{ ...MONO, ...MUTED, background: "var(--surface-ground)", borderColor: "var(--surface-border)", whiteSpace: "pre-wrap" }}
    >
      {refused.map((x) => `line ${x.line}, column ${x.column} · ${x.code}\n${x.message}`).join("\n")}
      {note && `\n${note}`}
    </span>
  );
}

// What left this machine and what did not, so the tester can say whether that is acceptable.
function SentComponent({
  flowId,
  recording,
  reviews,
  progress,
  request,
}: {
  flowId: number;
  recording: RecordingResultDto;
  reviews: Record<number, ActionReview>;
  progress: FlowGenerationProgressDto;
  request: string;
}) {
  const [isRequestShown, setIsRequestShown] = useState(false);

  // The same query Review reads the description through, so it is already there.
  const { data: flow } = useQuery({
    queryKey: ["flows", "recording-description", flowId],
    queryFn: () => backendApiService.Flow.get(flowId),
  });

  const listed = listedActions(recording);
  const choiceOf = (index: number) => reviews[index]?.choice;

  const labels = recording.clickedElements.filter((x) => x.text.trim().length > 0).length;
  const asWritten = listed.filter((x) => choiceOf(x.index) === ReviewChoiceEnum.TYPE_AS_WRITTEN).length;
  const screenshots = recording.actions.filter((x) => x.screenshotIndex != null).length;
  const inputs = listed
    .filter((x) => choiceOf(x.index) === ReviewChoiceEnum.INPUT)
    .map((x) => `{{${reviews[x.index].inputName}}}`);

  let script = "Being written";
  if (progress.scriptLength > 0) script = `${(progress.scriptLength / 1024).toFixed(1)} KB`;

  let readText = plural(labels, "label");
  if (!progress.isReadTextSent) readText = "Not sent";

  return (
    <section
      aria-labelledby="sent-heading"
      className="flex flex-column gap-3 p-4 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        id="sent-heading"
        className="m-0 text-base font-semibold"
      >
        Sent to {progress.model || "the model"}
      </h2>
      <ul className="list-none m-0 p-0 flex flex-column gap-2">
        <SentRowComponent label="Your description">{sentencesOf(flow?.description ?? "")}</SentRowComponent>
        <SentRowComponent label="The draft, as a script">{script}</SentRowComponent>
        <SentRowComponent label="Text read under each click">{readText}</SentRowComponent>
        {asWritten > 0 && <SentRowComponent label="Text typed as written">{plural(asWritten, "entry", "entries")}</SentRowComponent>}
        <SentRowComponent label="Which clicks hit the same picture">{plural(recording.clickGroups.length, "group")}</SentRowComponent>
      </ul>

      {!progress.isReadTextSent && (
        <span
          className="text-xs"
          style={MUTED}
        >
          Your provider may not be sent what is on screen, so the model names the steps from the description and
          their order. It is &quot;Send text read from the screen&quot;, under Settings, AI.
        </span>
      )}

      <div style={{ height: 1, background: "var(--surface-border)" }} />

      <h3
        className="m-0 text-sm font-semibold"
        style={MUTED}
      >
        Kept on this machine
      </h3>
      <ul
        className="list-none m-0 p-0 flex flex-column gap-2"
        style={MUTED}
      >
        <li>{plural(screenshots, "screenshot")} and the templates cut from them</li>
        {inputs.length > 0 && <li>What you typed: the model sees {inputs.join(", ")}</li>}
      </ul>

      <Button
        type="button"
        label={isRequestShown ? "Hide the exact request" : "Show the exact request"}
        onClick={() => setIsRequestShown(!isRequestShown)}
        disabled={!request}
        size="small"
        severity="secondary"
        outlined
        className="align-self-start"
      />
      {isRequestShown && request && (
        <pre
          className="m-0 p-3 border-round-lg text-xs overflow-auto"
          style={{ ...MONO, background: "var(--surface-ground)", maxHeight: 320, whiteSpace: "pre-wrap" }}
        >
          {request}
        </pre>
      )}
    </section>
  );
}

function SentRowComponent({ label, children }: { label: string; children: ReactNode }) {
  return (
    <li className="flex justify-content-between gap-3">
      <span style={MUTED}>{label}</span>
      <span
        className="text-xs text-right"
        style={{ ...MONO, ...MUTED }}
      >
        {children}
      </span>
    </li>
  );
}

// Seven, as the mockup has them: three done by the stop before Review, four by the generation.
function stagesOf(
  recording: RecordingResultDto,
  reviews: Record<number, ActionReview>,
  progress: FlowGenerationProgressDto,
  result: FlowGenerationResultDto | null,
): Stage[] {
  const listed = listedActions(recording);
  const choiceOf = (index: number) => reviews[index]?.choice;

  const leftOut = listed.filter((x) => choiceOf(x.index) === ReviewChoiceEnum.LEAVE_OUT).length;
  const labels = recording.clickedElements.filter((x) => x.text.trim().length > 0).length;
  const searched = listed.filter((x) => choiceOf(x.index) === ReviewChoiceEnum.WAIT_AND_CLICK).length;

  let cut = `From ${plural(searched, "click")}`;
  if (recording.clickGroups.length > 0) cut += "; the clicks on one picture share one";

  let attempt = "Naming the steps and grouping the repeated clicks, with your description";
  if (progress.attempt > 0) attempt += ` · attempt ${progress.attempt} of ${progress.attemptCount}`;

  const readBack: Stage = { title: "Read the script back", detail: "Parsed and checked the way an imported file is" };
  if (progress.refused.length > 0) {
    readBack.refused = progress.refused;
    readBack.refusedNote = `Sent back to the model with the lines. Attempt ${progress.attempt} of ${progress.attemptCount}.`;
  }

  const saved: Stage = { title: "Saved and validated", detail: "Imported into the flow, and checked as any flow is" };

  if (result?.isModelDraft) {
    readBack.detail = `Read cleanly on attempt ${result.attempts} of ${progress.attemptCount}`;
    saved.detail = validationOf(result);
  }

  if (result && !result.isModelDraft) {
    readBack.detail = result.reason;
    readBack.refused = result.refused;
    readBack.refusedNote = "The plain draft was saved instead.";
    saved.title = "Saved the plain draft";
    saved.detail = validationOf(result);
  }

  return [
    { title: "Folded the recording into actions", detail: `${plural(listed.length, "action")}; ${leftOut} left out by you` },
    { title: "Read the text under each click", detail: `${plural(labels, "label")}, read on this machine` },
    { title: "Cut the templates", detail: cut },
    { title: "Wrote the draft", detail: "Every wait sized from how long you waited" },
    { title: "Asked the model to shape it", detail: attempt },
    readBack,
    saved,
  ];
}

function activeIndexOf(progress: FlowGenerationProgressDto): number {
  if (progress.state === FlowGenerationStateEnum.DONE) return Number.MAX_SAFE_INTEGER;
  if (progress.stage === FlowGenerationStageEnum.SAVING) return 6;
  if (progress.stage === FlowGenerationStageEnum.ASKING_MODEL) return 4;
  return 3;
}

function statusOf(index: number, active: number): StageStatus {
  if (index < active) return "done";
  if (index === active) return "active";
  return "pending";
}

function validationOf(result: FlowGenerationResultDto): string {
  if (!result.import.isSuccess) return result.import.errors.map((x) => x.message).join(" ");

  const issues = result.import.validation?.issues ?? [];
  const errors = issues.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length;
  const warnings = issues.filter((x) => x.severity === ValidationSeverityEnum.WARNING).length;

  return `${plural(errors, "error")}, ${plural(warnings, "warning")}`;
}

function sentencesOf(text: string): string {
  const count = text.split(/[.!?]+(?:\s|$)/).filter((x) => x.trim().length > 0).length;
  if (count === 0) return "None written";

  return plural(count, "sentence");
}

function plural(count: number, one: string, many = `${one}s`): string {
  return `${count} ${count === 1 ? one : many}`;
}
