import { useEffect, useLayoutEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";
import { useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { SelectButton } from "primereact/selectbutton";
import { Tag } from "primereact/tag";

import { BroadcastTypeEnum } from "../../../../electron/shared/types";
import { ChoiceCardComponent } from "@/shared/components/ChoiceCardComponent";
import { RecordingGestureEnum } from "@/shared/enums/backend/recording-gesture-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import { SearchByEnum } from "@/shared/enums/backend/search-by-enum";
import { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";
import type { RecordingGestureDto } from "@/shared/models/recording-gesture-dto";
import type { RecordingProgressDto } from "@/shared/models/recording-progress-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import DiscordBotFormComponent from "@/features/discord-bot/components/form/DiscordBotFormComponent";
import { useDiscordBotMutations } from "@/features/discord-bot/hooks/use-discord-bot";
import CheckNotifyFieldsComponent from "@/features/recording/gesture/CheckNotifyFieldsComponent";
import CheckRuleFieldsComponent from "@/features/recording/gesture/CheckRuleFieldsComponent";
import StepFieldsComponent from "@/features/recording/gesture/StepFieldsComponent";
import {
  blankFields,
  type GestureFields,
  isComplete,
  isReadingChoice,
  STEP_OPTIONS,
  stepNote,
  toReviewedFields,
} from "@/features/recording/gesture/gesture";

const BOT_FORM_ID = "gesture-window-discord-bot-form";
const LAST_BOT_KEY = "stepinflow.recording.lastDiscordBotId";
const PREVIEW_HEIGHT = 128;

// Solid, because the window is see-through.
const PANEL: CSSProperties = {
  background: "var(--surface-overlay)",
  borderColor: "var(--surface-border)",
  boxShadow: "0 12px 32px rgba(0, 0, 0, 0.45)",
};

type Screen = "choice" | "form" | "bot";

interface Status {
  severity: "success" | "warn" | "info";
  text: string;
}

/**
 * The window a gesture asks in, while the recording is paused at it: what a Ctrl + click checks,
 * or which step a Ctrl + right click is. The choices first, one under the other, then the form of
 * the one picked, with a way back. Adding keeps the answer for Review to start from; Decide in
 * Review keeps none. Either resumes the recording, and the backend gives the keyboard back to the
 * window that had it.
 *
 * What a check pressed is found while this is open, and the window asks for it again once it is.
 */
export default function RecordingGesturePage() {
  const [searchParams] = useSearchParams();
  const pressIndex = Number(searchParams.get("pressIndex"));
  const isCheck = searchParams.get("gesture") === RecordingGestureEnum.CHECK;

  const queryClient = useQueryClient();
  const { createDiscordBotMutation } = useDiscordBotMutations();

  const { data: gesture } = useQuery({
    queryKey: ["recording", "gesture", pressIndex],
    queryFn: () => backendApiService.Recording.getGesture(),
    staleTime: Infinity,
  });

  const [screen, setScreen] = useState<Screen>("choice");
  const [edited, setEdited] = useState<GestureFields>(() => ({
    ...blankFields(isCheck ? ReviewChoiceEnum.CHECK_THERE : ReviewChoiceEnum.EXECUTE_COMMAND),
    discordBotId: lastBot(),
  }));
  const [isWordsEdited, setIsWordsEdited] = useState(false);
  const [tested, setTested] = useState<{ rule: string; passes: boolean } | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const rootRef = useRef<HTMLDivElement>(null);
  const readText = gesture?.element?.text || null;

  // The words read stand in for words never typed, however late they are read.
  const fields: GestureFields = isWordsEdited || !readText ? edited : { ...edited, searchText: readText };

  const change = (patch: Partial<GestureFields>) => {
    if (patch.searchText !== undefined) setIsWordsEdited(true);
    setEdited((x) => ({ ...x, ...patch }));
  };

  const isByWords = isCheck && fields.searchBy === SearchByEnum.TEXT;
  const isReading = !isCheck && screen === "form" && isReadingChoice(fields);
  const isTestable = isByWords && !!gesture?.isElementFound && !!readText;
  const rule = JSON.stringify([fields.conditionType, fields.searchText, fields.conditionTextEnd]);
  const passes = isTestable && tested?.rule === rule ? tested.passes : null;

  // Asks again once what it pressed has been found.
  useEffect(() => {
    const key = ["recording", "gesture", pressIndex];

    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.RECORDING_PROGRESS) return;

      const asking = (message.payload as RecordingProgressDto).asking;
      const shown = queryClient.getQueryData<RecordingGestureDto | null>(key);
      if (asking?.pressIndex === pressIndex && asking.isElementFound && !shown?.isElementFound)
        void queryClient.invalidateQueries({ queryKey: key });
    });

    return unsubscribe;
  }, [pressIndex, queryClient]);

  // Tested as an execution tests it, once there is something read to test.
  useEffect(() => {
    if (!isTestable) return;

    const [conditionType, text, textEnd] = JSON.parse(rule) as [GestureFields["conditionType"], string, string];
    const timer = window.setTimeout(() => {
      backendApiService.Recording.testGestureRule({ pressIndex, conditionType, text, textEnd })
        .then((result) => setTested({ rule, passes: result }))
        .catch(() => setTested(null));
    }, 250);

    return () => window.clearTimeout(timer);
  }, [isTestable, pressIndex, rule]);

  useLayoutEffect(() => {
    const root = rootRef.current;
    if (!root) return;

    const observer = new ResizeObserver(() => ElectronApiService.recordingGesture.resizeWindow(root.getBoundingClientRect().height));
    observer.observe(root);

    return () => observer.disconnect();
  }, []);

  const decide = async (isAdding: boolean) => {
    setIsBusy(true);
    setError(null);

    try {
      const answer = isAdding ? { ...toReviewedFields(fields), actionIndex: 0, isSecret: false } : null;
      await backendApiService.Recording.decideGesture({ pressIndex, answer });
      if (isAdding && fields.discordBotId !== null) rememberBot(fields.discordBotId);
    } catch (err) {
      // No longer asked: the recording resumed or stopped meanwhile, so there is nothing to keep.
      setError(err instanceof Error ? err.message : String(err));
    }

    await ElectronApiService.recordingGesture.closeWindow();
  };

  const canAdd = screen === "form" && isComplete(fields) && !isBusy;

  // Enter adds and Esc leaves it for Review, inside this window only.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape" && screen !== "bot") void decide(false);
      if (e.key === "Enter" && canAdd && !(e.target instanceof HTMLTextAreaElement)) void decide(true);
    };

    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

  const pickCheck = (searchBy: SearchByEnum) => {
    change({ searchBy });
    setScreen("form");
  };

  const pickStep = (choice: ReviewChoiceEnum) => {
    change({ choice });
    setScreen("form");
  };

  const saveBot = async (data: DiscordBotDto) => {
    const discordBotId = await createDiscordBotMutation.mutateAsync({ ...data, id: 0 });
    change({ discordBotId });
    setScreen("form");
  };

  const heading = headingOf(screen, isCheck, fields);

  return (
    <div
      ref={rootRef}
      className="p-2"
      style={{ width: "100%", color: "var(--text-color)" }}
    >
      <section
        role="dialog"
        aria-labelledby="gesture-title"
        className="flex flex-column border-1 border-round-xl overflow-hidden"
        style={PANEL}
      >
        <header className="flex align-items-start gap-3 px-4 pt-4 pb-3">
          {screen !== "choice" && (
            <Button
              type="button"
              icon="pi pi-arrow-left"
              aria-label={screen === "bot" ? "Back to the form" : "Back to the choices"}
              onClick={() => setScreen(screen === "bot" ? "form" : "choice")}
              severity="secondary"
              outlined
              className="flex-none"
            />
          )}
          <div className="flex flex-column gap-1 flex-1 min-w-0">
            <h2
              id="gesture-title"
              className="m-0 text-lg font-bold"
            >
              {heading.title}
            </h2>
            <p className="m-0 text-sm text-color-secondary line-height-3">{heading.subtitle}</p>
          </div>
          <Tag
            value="Paused"
            icon="pi pi-pause"
            severity="warning"
            className="flex-none"
          />
        </header>

        <div className="flex flex-column gap-3 px-4 pb-4">
          {((isCheck && screen !== "bot") || isReading) && (
            <PreviewComponent
              gesture={gesture}
              caption={captionOf(screen, fields)}
            />
          )}

          {screen === "choice" && isCheck && (
            <>
              <ChoiceCardComponent
                icon="image"
                title="By its picture"
                description="Waits for what you clicked to look as it does now. You set the exact crop in Review, after you stop."
                onSelect={() => pickCheck(SearchByEnum.IMAGE)}
              />
              <ChoiceCardComponent
                icon="align-left"
                title="By its words"
                description="Waits for the words read on it, or for text that follows a rule: contains, a number above, a pattern."
                onSelect={() => pickCheck(SearchByEnum.TEXT)}
              />
            </>
          )}

          {screen === "choice" &&
            !isCheck &&
            STEP_OPTIONS.map((option) => (
              <ChoiceCardComponent
                key={option.value}
                icon={option.icon}
                title={option.label}
                description={option.description}
                onSelect={() => pickStep(option.value)}
              />
            ))}

          {screen === "form" && isCheck && (
            <>
              <div className="flex align-items-center justify-content-between flex-wrap gap-3">
                <span
                  id="gesture-until"
                  className="font-medium"
                >
                  The flow waits
                </span>
                <SelectButton
                  aria-labelledby="gesture-until"
                  value={fields.choice}
                  options={[
                    { label: "Until it is there", value: ReviewChoiceEnum.CHECK_THERE },
                    { label: "Until it has gone", value: ReviewChoiceEnum.CHECK_GONE },
                  ]}
                  optionLabel="label"
                  optionValue="value"
                  allowEmpty={false}
                  onChange={(e) => {
                    if (e.value) change({ choice: e.value });
                  }}
                />
              </div>

              {!isByWords && (
                <Message
                  severity="info"
                  className="w-full justify-content-start"
                  text="The template is the element found under your click, cut from the screenshot taken just before it. You set its exact edges in Review, after you stop."
                />
              )}

              {isByWords && (
                <>
                  <CheckRuleFieldsComponent
                    fields={fields}
                    onChange={change}
                    readText={readText}
                  />
                  <RuleStatusComponent status={ruleStatus(fields, gesture, readText, passes)} />
                </>
              )}

              <CheckNotifyFieldsComponent
                fields={fields}
                onChange={change}
                onNewBot={() => setScreen("bot")}
              />
            </>
          )}

          {screen === "form" && !isCheck && (
            <>
              <StepFieldsComponent
                fields={fields}
                onChange={change}
                onNewBot={() => setScreen("bot")}
              />
              {isReading && (
                <Message
                  severity="info"
                  className="w-full justify-content-start"
                  text={readingNote(gesture, readText)}
                />
              )}
              <span className="text-xs text-color-secondary">{stepNote(fields)}</span>
            </>
          )}

          {screen === "bot" && (
            <DiscordBotFormComponent
              formId={BOT_FORM_ID}
              formMode="ADD"
              defaultValues={new DiscordBotDto()}
              isFormInDialog={true}
              onEdit={() => setScreen("form")}
              onCancel={() => setScreen("form")}
              onSubmit={(data) => void saveBot(data)}
            />
          )}

          {error && (
            <Message
              severity="error"
              className="w-full justify-content-start"
              text={error}
            />
          )}
        </div>

        <footer
          className="flex align-items-center gap-2 px-4 py-3 border-top-1"
          style={{ borderColor: "var(--surface-border)", background: "var(--surface-ground)" }}
        >
          {screen === "choice" && (
            <>
              <span className="flex-1" />
              <Button
                type="button"
                label="Decide in Review and keep recording"
                onClick={() => void decide(false)}
                disabled={isBusy}
                severity="secondary"
                outlined
              />
            </>
          )}

          {screen === "form" && (
            <>
              <Button
                type="button"
                label="Decide in Review"
                onClick={() => void decide(false)}
                disabled={isBusy}
                severity="secondary"
                text
              />
              <span className="flex-1" />
              <Button
                type="button"
                icon="pi pi-check"
                label={isCheck ? "Add check and keep recording" : "Add step and keep recording"}
                onClick={() => void decide(true)}
                disabled={!canAdd}
                loading={isBusy}
              />
            </>
          )}

          {screen === "bot" && (
            <>
              <Button
                type="button"
                label="Back to the form"
                onClick={() => setScreen("form")}
                severity="secondary"
                text
              />
              <span className="flex-1" />
              <Button
                type="submit"
                form={BOT_FORM_ID}
                icon="pi pi-check"
                label="Save the bot and use it"
                loading={createDiscordBotMutation.isPending}
              />
            </>
          )}
        </footer>
      </section>
    </div>
  );
}

// The main area just before the press, around where it was pressed, with the element once found.
function PreviewComponent({ gesture, caption }: { gesture: RecordingGestureDto | null | undefined; caption: string }) {
  const element = gesture?.element ?? null;
  const clickX = (gesture?.locationX ?? 0) - (gesture?.x ?? 0);
  const clickY = (gesture?.locationY ?? 0) - (gesture?.y ?? 0);
  const left = (x: number) => `calc(50% - ${clickX - x}px)`;
  const top = (y: number) => `${PREVIEW_HEIGHT / 2 - clickY + y}px`;

  return (
    <figure className="m-0 flex flex-column gap-2">
      <figcaption className="text-xs font-semibold uppercase text-color-secondary">{caption}</figcaption>
      <div
        className="relative overflow-hidden border-1 border-round-lg"
        style={{ height: PREVIEW_HEIGHT, borderColor: "var(--surface-border)", background: "var(--surface-ground)" }}
      >
        {gesture?.image ? (
          <>
            <img
              src={`data:image/png;base64,${gesture.image}`}
              alt="Where you clicked, just before the click"
              className="absolute"
              style={{ left: left(0), top: top(0), maxWidth: "none" }}
            />
            {element && (
              <span
                aria-hidden="true"
                className="absolute border-round"
                style={{ left: left(element.x), top: top(element.y), width: element.width, height: element.height, border: "2px dashed #4F63E6" }}
              />
            )}
            <span
              aria-hidden="true"
              className="absolute border-circle"
              style={{ left: "calc(50% - 15px)", top: PREVIEW_HEIGHT / 2 - 15, width: 30, height: 30, border: "3px solid #E5484D" }}
            />
          </>
        ) : (
          <span className="flex align-items-center justify-content-center h-full text-sm text-color-secondary">
            No screenshot of where you clicked.
          </span>
        )}

        <span className="absolute" style={{ top: 8, right: 8 }}>
          {gesture?.isElementFound ? (
            <Tag
              value={element ? "Found what you clicked" : "Nothing found: a box around the click"}
              icon={element ? "pi pi-check" : "pi pi-info-circle"}
              severity={element ? "success" : "info"}
            />
          ) : (
            <Tag
              value="Finding what you clicked..."
              icon="pi pi-spin pi-spinner"
              severity="info"
            />
          )}
        </span>
      </div>
    </figure>
  );
}

function RuleStatusComponent({ status }: { status: Status }) {
  return (
    <Message
      severity={status.severity}
      className="w-full justify-content-start"
      text={status.text}
    />
  );
}

function ruleStatus(fields: GestureFields, gesture: RecordingGestureDto | null | undefined, readText: string | null, passes: boolean | null): Status {
  if (!gesture?.isElementFound) return { severity: "info", text: "Tested against the screen as soon as the words on it are read." };
  if (!readText) return { severity: "warn", text: "Nothing was read on what you clicked, so the rule cannot be tested on the screen as it is now." };
  if (passes === null) return { severity: "info", text: "Testing it against the screen..." };

  const isThere = fields.choice === ReviewChoiceEnum.CHECK_THERE;
  if (isThere && passes) return { severity: "success", text: `Passes on the screen as it is now: “${readText}” was read on it.` };
  if (isThere) return { severity: "warn", text: `“${readText}” does not pass it, so an execution would wait for it and fail if it never comes.` };
  if (passes) return { severity: "info", text: "It passes now. The execution waits here until it no longer does." };

  return { severity: "warn", text: "It does not pass now, so the check would pass at once." };
}

function headingOf(screen: Screen, isCheck: boolean, fields: GestureFields): { title: string; subtitle: string } {
  if (screen === "bot") return { title: "New Discord bot", subtitle: "A channel to post to. Notify steps pick one of these." };

  if (isCheck && screen === "choice")
    return { title: "Add a check here", subtitle: "The recording is paused. How should the flow check what you Ctrl + clicked?" };

  if (isCheck && fields.searchBy === SearchByEnum.TEXT)
    return { title: "Check it by its words", subtitle: "Waits for the words read on it, or for text that follows a rule." };

  if (isCheck) return { title: "Check it by its picture", subtitle: "Waits for what you clicked to look as it does now." };

  if (screen === "choice")
    return {
      title: "Add a step here",
      subtitle:
        "The recording is paused. The step goes after the last thing you did. Where you right clicked matters only to a notification that reads the text there.",
    };

  const option = STEP_OPTIONS.find((x) => x.value === fields.choice);
  return { title: option?.label ?? "A step", subtitle: option?.description ?? "" };
}

function captionOf(screen: Screen, fields: GestureFields): string {
  if (screen === "choice") return "What you Ctrl + clicked";
  if (fields.searchBy === SearchByEnum.TEXT || isReadingChoice(fields)) return "Where it reads";

  return "The template, cut from the screenshot before your click";
}

function readingNote(gesture: RecordingGestureDto | null | undefined, readText: string | null): string {
  const later = "You can move and resize the area in Review, after you stop.";

  if (!gesture?.isElementFound) return `It reads inside the area found around where you right clicked. ${later}`;
  if (!gesture.element) return "You right clicked outside the window the flow works in, so there is nothing to read there.";
  if (!readText) return `Nothing reads in the area right now. ${later}`;

  return `It reads “${readText}” there now. ${later}`;
}

// The bot the tester used last, so most notifications never ask for one.
function lastBot(): number | null {
  try {
    const stored = window.localStorage.getItem(LAST_BOT_KEY);
    return stored ? Number(stored) : null;
  } catch {
    return null;
  }
}

function rememberBot(discordBotId: number): void {
  try {
    window.localStorage.setItem(LAST_BOT_KEY, String(discordBotId));
  } catch {
    // Remembered only for convenience.
  }
}
