import { Fragment, useEffect, useState } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { ProgressBar } from "primereact/progressbar";

import { BroadcastTypeEnum } from "../../../../electron/shared/types";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import { RecordingStageEnum } from "@/shared/enums/backend/recording-stage-enum";
import { FlowGenerationStageEnum } from "@/shared/enums/backend/flow-generation-stage-enum";
import { FlowGenerationStateEnum } from "@/shared/enums/backend/flow-generation-state-enum";
import type { RecordingProgressDto } from "@/shared/models/recording-progress-dto";
import type { RecordingResultDto } from "@/shared/models/recording-result-dto";
import type { FlowGenerationProgressDto } from "@/shared/models/flow-generation-progress-dto";
import type { FlowGenerationResultDto } from "@/shared/models/flow-generation-result-dto";
import RecordingSetupComponent from "@/features/recording/components/RecordingSetupComponent";
import RecordingMainAreaComponent from "@/features/recording/components/RecordingMainAreaComponent";
import RecordingReviewComponent from "@/features/recording/review/RecordingReviewComponent";
import RecordingGeneratingComponent from "@/features/recording/generate/RecordingGeneratingComponent";
import RecordingCheckComponent, { type RevisionProblem } from "@/features/recording/check/RecordingCheckComponent";
import { type ActionReview, toReviewed } from "@/features/recording/review/review";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import RecordingLiveListComponent from "@/features/recording/components/RecordingLiveListComponent";
import { useRecordingStore } from "@/features/recording/store/recording-store";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { flowKeys, useFlow } from "@/features/flow/hooks/use-flow";
import { AI_OFF, useAiStatus } from "@/features/ai/hooks/use-ai";

const RECORDING_STEPS = ["Describe", "Record", "Review", "Generate", "Check"];

// Until the first stage says which model and what it is sent. A revision starts the same way.
const GENERATION_STARTING: FlowGenerationProgressDto = {
  state: FlowGenerationStateEnum.GENERATING,
  stage: FlowGenerationStageEnum.WRITING_DRAFT,
  model: "",
  isReadTextSent: true,
  scriptLength: 0,
  attempt: 0,
  attemptCount: 0,
  refused: [],
};

const STAGE_LABELS: Record<RecordingStageEnum, string> = {
  NONE: "Getting the review ready",
  FINDING_ELEMENTS: "Finding what you clicked",
  GROUPING_CLICKS: "Grouping the clicks that hit the same picture",
  READING_SCREENS: "Reading what changed on the screen",
};

/**
 * Records what the user does and shows it arriving, folded into actions.
 *
 * Record now opens the recording bar and minimises this window, and the bar's Start starts the
 * recording, so the tester can arrange their windows first.
 *
 * The page only asks; the backend says what happened, as RECORDING_PROGRESS, and the page follows
 * it. A stop from here, the bar or its hotkey is the same stop: the backend works the recording
 * through, stage by stage, then says READY, and the page fetches the result and shows Review.
 * A discard from anywhere comes back as NONE, and the page leaves. The bar closes once the
 * recording stops.
 *
 * A new flow is described first, and the plain draft is imported into it whole. From the editor's
 * "Record steps here" the flow already exists: it records against its main area, asking for the
 * window when it has none, and the draft is inserted at the editor's position and opens there.
 *
 * Generate flow hands the reviewed recording to the model set up in Settings, in the background:
 * FLOW_GENERATION_PROGRESS says how far, and once it says DONE the page fetches what was saved -
 * the model's draft, or the plain one and why.
 *
 * Either draft is read on Check before the editor opens. Its "Ask for changes" is followed the
 * way a generation is, and an answer that is not saved leaves Check showing what was.
 *
 * The live list is lines, never pixels; Review asks for an image when it draws that action.
 */
export default function RecordingPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { target, createAsSubFlow, setupFlowId, setSetupFlowId, reset } = useRecordingStore();
  const { setSelectedTreeNode } = useWorkflowStore();
  const { openConfirm } = useDialogStore();
  const { data: isAiConfigured } = useAiStatus();

  const { data: targetFlow } = useFlow(target?.flowId ?? null);
  const mainArea = targetFlow?.flowAreas.find((x) => x.isMain);
  const needsMainArea = targetFlow !== undefined && mainArea === undefined;

  // The flow the recording is made against, once it has a main area.
  const recordingFlowId = setupFlowId ?? (mainArea ? target?.flowId : undefined);

  // A new QA test is generated by the model only. Steps recorded into one that exists are inserted
  // as a plain draft, as into any flow.
  const { data: setupFlow } = useFlow(setupFlowId ?? null);
  const isQaTest = setupFlow?.isQaTest === true;

  // The recording this page follows, null until one is underway, so the NONE of a discard made
  // before any recording - this page's own Cancel - is not taken for one.
  const [progress, setProgress] = useState<RecordingProgressDto | null>(null);
  const [result, setResult] = useState<RecordingResultDto | null>(null);
  const [isContinuing, setIsContinuing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // The generation this page started, null until it asks for one, and what Review said beside it.
  const [generation, setGeneration] = useState<FlowGenerationProgressDto | null>(null);
  const [generated, setGenerated] = useState<FlowGenerationResultDto | null>(null);
  const [generatedFrom, setGeneratedFrom] = useState<Record<number, ActionReview> | null>(null);

  // What Check shows, and an "Ask for changes" underway or the last one that was not saved.
  const [checked, setChecked] = useState<FlowGenerationResultDto | null>(null);
  const [revision, setRevision] = useState<FlowGenerationProgressDto | null>(null);
  const [revisionProblem, setRevisionProblem] = useState<RevisionProblem | null>(null);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type === BroadcastTypeEnum.RECORDING_PROGRESS) {
        const received = message.payload as RecordingProgressDto;
        setProgress((current) => (current || isUnderway(received) ? received : current));
      }

      if (message.type === BroadcastTypeEnum.FLOW_GENERATION_PROGRESS) {
        const received = message.payload as FlowGenerationProgressDto;
        setGeneration((current) => (current ? received : current));
        setRevision((current) => (current ? received : current));
      }
    });

    return unsubscribe;
  }, []);

  // The bar never outlives the page that opened it.
  useEffect(() => {
    return () => {
      void ElectronApiService.recordingBar.closeWindow();
    };
  }, []);

  const leave = async () => {
    await ElectronApiService.recordingBar.closeWindow();
    await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });

    reset();
    navigate(-1);
  };

  const openReview = async (ready: RecordingProgressDto) => {
    if (ready.errorMessage) console.error(ready.errorMessage);

    try {
      const recording = await backendApiService.Recording.getResult();

      if (recording.actions.length === 0) {
        setProgress(null);
        setError("Nothing was recorded. Start again and perform the task you want automated.");
        return;
      }

      setResult(recording);
    } catch (err) {
      setProgress(null);
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // The plain draft: imported whole into the flow the setup made and read on Check, or inserted at
  // the editor's position and opened there. A draft that does not read cleanly leaves the flow as it
  // was and says where.
  const continueFromReview = async (reviews: Record<number, ActionReview>) => {
    if (!result || recordingFlowId === undefined) return;

    setIsContinuing(true);
    setError(null);

    try {
      await backendApiService.Recording.review(toReviewed(result, reviews));

      if (target) {
        const inserted = await backendApiService.Recording.insertDraft(target);
        if (!inserted.isSuccess) {
          setError(`The draft could not be written into the flow. ${linesOf(inserted.errors)}`);
          return;
        }

        await openFlow(inserted.flowId);
        return;
      }

      const imported = await backendApiService.Recording.importDraft(recordingFlowId);
      if (!imported.import.isSuccess) {
        setError(`The draft could not be written into the flow. ${linesOf(imported.import.errors)}`);
        return;
      }

      setChecked(imported);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsContinuing(false);
    }
  };

  const openFlow = async (flowId: number) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["flow"] }),
      queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
      queryClient.invalidateQueries({ queryKey: ["flows"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

    // The placeholder it was recorded from is gone from the tree.
    if (target) setSelectedTreeNode(undefined);

    reset();
    navigate(`/workflow/${flowId}`);
  };

  // Started and followed: the request only starts it, as a model takes longer than one can wait.
  const generateFromReview = async (reviews: Record<number, ActionReview>) => {
    if (!result || recordingFlowId === undefined) return;

    setIsContinuing(true);
    setError(null);

    try {
      await backendApiService.Recording.review(toReviewed(result, reviews));

      setGeneratedFrom(reviews);
      setGenerated(null);
      setGeneration(GENERATION_STARTING);

      await backendApiService.Recording.generateDraft(recordingFlowId);
    } catch (err) {
      setGeneration(null);
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsContinuing(false);
    }
  };

  // Back to Review. Nothing is saved: the NONE the cancel comes back as says so.
  const cancelGeneration = async () => {
    try {
      await backendApiService.Recording.cancelDraftGeneration();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }

    setGeneration(null);
  };

  const fetchGenerated = async () => {
    try {
      setGenerated(await backendApiService.Recording.getGeneratedDraft());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // Ended, from whichever stage it was in.
  useEffect(() => {
    if (generation?.state === FlowGenerationStateEnum.NONE) setGeneration(null);
    if (generation?.state === FlowGenerationStateEnum.DONE) void fetchGenerated();
  }, [generation?.state]);

  const checkGenerated = () => {
    if (!generated) return;

    setChecked(generated);
    setGeneration(null);
  };

  // Check's "Ask for changes": started and followed as a generation is.
  const askForChanges = async (changes: string) => {
    if (!checked) return;

    setError(null);
    setRevisionProblem(null);
    setRevision(GENERATION_STARTING);

    try {
      await backendApiService.Recording.reviseDraft({ flowId: checked.import.flowId, changes });
    } catch (err) {
      setRevision(null);
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // Nothing is saved: the NONE the cancel comes back as finds no revision to follow.
  const cancelAsk = async () => {
    try {
      await backendApiService.Recording.cancelDraftGeneration();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }

    setRevision(null);
  };

  // A revision that was not saved leaves Check as it was, and says why beside the ask.
  const fetchRevised = async (done: FlowGenerationProgressDto) => {
    if (done.errorMessage) {
      setRevisionProblem({ message: done.errorMessage, refused: [] });
      return;
    }

    try {
      const revised = await backendApiService.Recording.getGeneratedDraft();
      if (!revised) return;

      if (revised.isModelDraft && revised.import.isSuccess) {
        await queryClient.invalidateQueries({ queryKey: ["flowStep"] });
        setChecked(revised);
        return;
      }

      let message = revised.reason;
      if (!message) message = `The answer could not be saved, so the flow is as it was. ${linesOf(revised.import.errors)}`;

      setRevisionProblem({ message, refused: revised.refused });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  useEffect(() => {
    if (revision?.state === FlowGenerationStateEnum.NONE) setRevision(null);

    if (revision?.state === FlowGenerationStateEnum.DONE) {
      void fetchRevised(revision);
      setRevision(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [revision?.state]);

  // The flow goes, its steps with it, and the recording after it.
  const discardChecked = () => {
    if (!checked) return;

    openConfirm("recording-flow-discard", {
      headerText: `Discard ${checked.import.flowName}?`,
      confirmLabel: "Discard the flow",
      confirmSeverity: "danger",
      children: <LabelComponent text="Its steps and templates go with it, and so does the recording. This cannot be undone." />,
      onConfirm: async () => {
        await backendApiService.Flow.delete(checked.import.flowId);
        await handleCancel();
      },
    });
  };

  let generateUnavailable: string | null = null;
  if (!isAiConfigured) generateUnavailable = AI_OFF;
  if (target) generateUnavailable = "A model's draft cannot go into a flow that already exists yet.";

  let askUnavailable: string | null = null;
  if (!isAiConfigured) askUnavailable = AI_OFF;

  const recordAgain = () => {
    if (recordingFlowId === undefined) return;

    setResult(null);
    setProgress(null);
    void openBar(recordingFlowId, setupFlowId);
  };

  // Stopped or discarded, from wherever it was.
  useEffect(() => {
    if (!progress || isLive(progress)) return;

    void ElectronApiService.recordingBar.closeWindow();

    if (progress.state === RecordingStateEnum.READY) void openReview(progress);
    if (progress.state === RecordingStateEnum.NONE) void leave();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [progress?.state]);

  // A discard made from the bar deletes the flow the setup created. One recorded into from the
  // editor stays.
  const openBar = async (flowId: number, createdFlowId: number | undefined) => {
    setError(null);

    try {
      await ElectronApiService.recordingBar.openWindow(flowId, createdFlowId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // The flow is created before anything is recorded, not on save: the recording is made against
  // its main area, and the name is reserved. The recorded steps go after the launch step.
  const handleSetupCreated = (createdFlowId: number) => {
    setSetupFlowId(createdFlowId);
    void queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
    void openBar(createdFlowId, createdFlowId);
  };

  const handleMainAreaSaved = (intoFlowId: number) => {
    void queryClient.invalidateQueries({ queryKey: flowKeys.detail(intoFlowId) });
    void openBar(intoFlowId, undefined);
  };

  const handleStop = async () => {
    setError(null);

    try {
      await backendApiService.Recording.stop();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  // Following a recording, the page leaves on the NONE the discard comes back as.
  const handleCancel = async () => {
    const isFollowing = progress !== null;

    try {
      await ElectronApiService.recordingBar.closeWindow();
      await backendApiService.Recording.discard(setupFlowId);
    } catch (err) {
      console.error(err);
      await leave();
      return;
    }

    if (!isFollowing) await leave();
  };

  const isRecording = progress !== null && isLive(progress);
  const isStopped = progress !== null && !isLive(progress);
  const actionCount = progress?.actionCount ?? 0;
  const recentActions = progress?.recentActions ?? [];

  return (
    <div
      className="m-4 flex flex-column gap-4"
      style={{ maxWidth: 1180 }}
    >
      <div className="flex align-items-start justify-content-between flex-wrap gap-3">
        <div className="flex flex-column gap-1">
          <h1
            className="m-0 text-3xl font-semibold"
            style={{ letterSpacing: "-0.02em" }}
          >
            {target ? `Record steps into ${targetFlow?.name ?? "the flow"}` : "Record a flow"}
          </h1>
          <p
            className="m-0"
            style={{ color: "var(--text-color-secondary)", maxWidth: 680 }}
          >
            {target
              ? "Do the task once. The steps go where you were in the editor, and nothing you do becomes a step until you have reviewed it."
              : "Tell it what the flow is for and which window it works in. Then do the task once; nothing you do becomes a step until you have reviewed it."}
          </p>
        </div>

        <RecordingStepsComponent
          current={stepOf(recordingFlowId !== undefined, result !== null, generation !== null, checked !== null)}
        />
      </div>

      {recordingFlowId !== undefined && !result && (
        <div className="flex align-items-center justify-content-end flex-wrap gap-2">
          {!progress && (
            <Button
              type="button"
              label="Record now"
              icon="pi pi-circle-fill"
              onClick={() => void openBar(recordingFlowId, setupFlowId)}
              severity="danger"
            />
          )}

          {progress && (
            <Button
              type="button"
              label={isRecording ? "Stop and review" : "Getting the review ready..."}
              icon="pi pi-stop-circle"
              loading={isStopped}
              onClick={handleStop}
            />
          )}

          <Button
            type="button"
            label="Cancel"
            onClick={handleCancel}
            severity="secondary"
            text
          />
        </div>
      )}

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      {result && !generation && !checked && (
        <RecordingReviewComponent
          result={result}
          flowId={recordingFlowId}
          elapsedMilliseconds={progress?.elapsedMilliseconds ?? 0}
          isContinuing={isContinuing}
          isQaTest={isQaTest}
          generateUnavailable={generateUnavailable}
          onRecordAgain={recordAgain}
          onContinue={(reviews) => void continueFromReview(reviews)}
          onGenerate={(reviews) => void generateFromReview(reviews)}
        />
      )}

      {result && generation && generatedFrom && recordingFlowId !== undefined && (
        <RecordingGeneratingComponent
          flowId={recordingFlowId}
          recording={result}
          reviews={generatedFrom}
          progress={generation}
          result={generated}
          onCancel={() => void cancelGeneration()}
          onBack={() => setGeneration(null)}
          onCheck={checkGenerated}
        />
      )}

      {checked && (
        <RecordingCheckComponent
          result={checked}
          revision={revision}
          revisionProblem={revisionProblem}
          askUnavailable={askUnavailable}
          onAsk={(changes) => void askForChanges(changes)}
          onCancelAsk={() => void cancelAsk()}
          onDiscard={discardChecked}
          onOpen={() => void openFlow(checked.import.flowId)}
        />
      )}

      {isRecording && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text="Recording. The bar at the top of the screen pauses and stops it, and nothing done on it is recorded."
        />
      )}

      {progress && isStopped && !result && (
        <div className="flex flex-column gap-2">
          <LabelComponent
            text={describeStage(progress)}
            size="sm"
            color="secondary"
          />
          <ProgressBar
            value={percentOf(progress)}
            // A stage that cannot be counted ahead moves rather than sitting at zero.
            mode={progress.stageTotal ? "determinate" : "indeterminate"}
            showValue={false}
            style={{ height: "0.5rem" }}
          />
        </div>
      )}

      {!target && setupFlowId === undefined && (
        <RecordingSetupComponent
          isSubFlow={createAsSubFlow}
          onCreated={handleSetupCreated}
          onCancel={() => void handleCancel()}
        />
      )}

      {target && needsMainArea && (
        <RecordingMainAreaComponent
          flowId={target.flowId}
          onSaved={() => handleMainAreaSaved(target.flowId)}
          onCancel={() => void handleCancel()}
        />
      )}

      {recordingFlowId !== undefined && !result && (
        <Panel header={`Actions (${actionCount})`}>
          {recentActions.length === 0 ? (
            <LabelComponent
              text={isRecording ? "Waiting for your first action..." : "Nothing recorded yet."}
              size="sm"
              color="secondary"
            />
          ) : (
            <RecordingLiveListComponent actions={recentActions} />
          )}
        </Panel>
      )}
    </div>
  );
}

// The plain draft skips Generate, which shows as passed all the same.
function RecordingStepsComponent({ current }: { current: number }) {
  return (
    <ol
      aria-label="Progress"
      className="flex align-items-center flex-wrap gap-2 list-none m-0 p-0 text-sm"
    >
      {RECORDING_STEPS.map((step, index) => {
        const isCurrent = index === current;
        const isDone = index < current;

        let circle: CSSProperties = { border: "1px solid var(--surface-border)" };
        if (isCurrent) circle = { background: "var(--primary-color)", color: "var(--primary-color-text)" };

        let line = "var(--surface-border)";
        if (isDone) line = "var(--primary-color)";

        return (
          <Fragment key={step}>
            {index > 0 && (
              <li
                aria-hidden="true"
                style={{ width: 20, height: 1, background: line }}
              />
            )}
            <li
              aria-current={isCurrent ? "step" : undefined}
              className="flex align-items-center gap-2"
              style={{ color: isCurrent || isDone ? "var(--text-color)" : "var(--text-color-secondary)" }}
            >
              <span
                className="flex align-items-center justify-content-center border-circle text-xs font-semibold"
                style={{ width: 22, height: 22, ...circle }}
              >
                {index + 1}
              </span>
              {step}
            </li>
          </Fragment>
        );
      })}
    </ol>
  );
}

function stepOf(isDescribed: boolean, isRecorded: boolean, isGenerating: boolean, isChecking: boolean): number {
  if (isChecking) return 4;
  if (isGenerating) return 3;
  if (isRecorded) return 2;
  if (isDescribed) return 1;
  return 0;
}

function isLive(progress: RecordingProgressDto): boolean {
  return progress.state === RecordingStateEnum.RECORDING || progress.state === RecordingStateEnum.PAUSED;
}

function isUnderway(progress: RecordingProgressDto): boolean {
  return isLive(progress) || progress.state === RecordingStateEnum.PROCESSING;
}

// "Finding what you clicked: 44 of 90", and which stage of how many once there is more than one.
function describeStage(progress: RecordingProgressDto): string {
  let line = STAGE_LABELS[progress.stage];
  if (progress.stageTotal) line += `: ${progress.stageDone} of ${progress.stageTotal}`;
  if (progress.stageCount > 1) line = `${progress.stageNumber} of ${progress.stageCount} · ${line}`;

  return line;
}

function linesOf(errors: FlowScriptErrorDto[]): string {
  return errors.map((x) => `line ${x.line}: ${x.message}`).join(" ");
}

function percentOf(progress: RecordingProgressDto): number {
  if (!progress.stageTotal) return 0;
  return Math.round((progress.stageDone * 100) / progress.stageTotal);
}
