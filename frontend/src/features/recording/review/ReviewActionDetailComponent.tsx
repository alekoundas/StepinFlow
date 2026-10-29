import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { Checkbox } from "primereact/checkbox";
import { InputText } from "primereact/inputtext";
import { SelectButton } from "primereact/selectbutton";

import { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import { ElementFoundByEnum } from "@/shared/enums/backend/element-found-by-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import { SearchByEnum } from "@/shared/enums/backend/search-by-enum";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { RecordingResultDto } from "@/shared/models/recording-result-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import ElementRectangleEditorComponent from "@/features/recording/review/ElementRectangleEditorComponent";
import {
  type ActionReview,
  elementOf,
  fieldLabelBefore,
  groupOf,
  isByWords,
  isOutside,
  isPointed,
  isTemplateChoice,
  listedActions,
  optionsFor,
  titleOf,
  toSeconds,
  waitFor,
} from "@/features/recording/review/review";

const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const SEARCH_BY_OPTIONS = [
  { label: "Its picture", value: SearchByEnum.IMAGE },
  { label: "Its words", value: SearchByEnum.TEXT },
];

interface Props {
  result: RecordingResultDto;
  action: RecordedActionDto;
  review: ActionReview;
  onChange: (review: ActionReview) => void;

  // Tells one recording's screens from the next one's.
  recordingKey: string;
  isQaTest: boolean;
}

/**
 * One action: the screen at that moment with where it was clicked and the template to be cut,
 * what is known about it, and what it becomes.
 */
export default function ReviewActionDetailComponent({ result, action, review, onChange, recordingKey, isQaTest }: Props) {
  const element = elementOf(result, action);
  const options = optionsFor(action, element, isQaTest);
  const position = listedActions(result).findIndex((x) => x.index === action.index) + 1;

  const { data: screen } = useQuery({
    queryKey: ["recording", "screen", recordingKey, action.index],
    queryFn: () => backendApiService.Recording.getScreen(action.index),
    staleTime: Infinity,
  });

  const isOwnScreen = screen?.actionIndex === action.index;

  let click: { x: number; y: number } | null = null;
  if (screen && isOwnScreen && isPointed(action)) click = { x: action.locationX - screen.x, y: action.locationY - screen.y };

  const isTemplate = isTemplateChoice(review.choice);

  return (
    <section
      aria-labelledby="review-detail-heading"
      className="flex flex-column gap-3"
      style={{ flex: "2 1 560px", minWidth: 0 }}
    >
      <div
        className="flex flex-column gap-3 p-3 border-1 border-round-xl"
        style={CARD}
      >
        <div className="flex align-items-center justify-content-between flex-wrap gap-3">
          <h2
            id="review-detail-heading"
            className="m-0 text-sm font-semibold"
          >
            {position} · {titleOf(action, element)}
          </h2>
          <span
            className="text-xs"
            style={MUTED}
          >
            {isOwnScreen ? "The screen at the moment you pressed" : "The screen at the last click before it"}
          </span>
        </div>

        {screen ? (
          <ElementRectangleEditorComponent
            imageSrc={`data:image/png;base64,${screen.image}`}
            width={screen.width}
            height={screen.height}
            click={click}
            element={isTemplate && isOwnScreen ? review.element : null}
            isEditable={isTemplate && isOwnScreen}
            onChange={(next) => onChange({ ...review, element: next })}
          />
        ) : (
          <span
            className="text-sm py-4 text-center"
            style={MUTED}
          >
            No screenshot: this was recorded without a window to work in.
          </span>
        )}

        <div
          className="flex flex-wrap gap-4 text-xs"
          style={MUTED}
        >
          <span className="flex align-items-center gap-2">
            <span
              className="border-circle"
              style={{ width: 12, height: 12, border: "2px solid #E5484D" }}
            />
            Where you clicked
          </span>
          <span className="flex align-items-center gap-2">
            <span style={{ width: 14, height: 10, border: "2px dashed #4F63E6" }} />
            {isByWords(review)
              ? "The area it reads the words in. Drag a corner to change it."
              : "The template it searches for. Drag a corner to change it."}
          </span>
        </div>
      </div>

      <div
        className="flex flex-column gap-3 p-4 border-1 border-round-xl"
        style={CARD}
      >
        <dl
          className="m-0"
          style={{ display: "grid", gridTemplateColumns: "minmax(140px, auto) 1fr", gap: "10px 18px" }}
        >
          {factsOf(result, action, review).map((fact) => (
            <FactComponent
              key={fact.name}
              name={fact.name}
              value={fact.value}
            />
          ))}
        </dl>

        <div className="flex flex-column gap-2">
          <span className="font-medium">Becomes</span>
          <SelectButton
            value={review.choice}
            options={options}
            optionLabel="label"
            optionValue="value"
            onChange={(e) => {
              if (e.value) onChange({ ...review, choice: e.value });
            }}
            allowEmpty={false}
            className="align-self-start"
          />
        </div>

        {isTemplate && isOwnScreen && (
          <div className="flex align-items-end flex-wrap gap-3">
            <div className="flex flex-column gap-2">
              <span className="font-medium">Find it by</span>
              <SelectButton
                value={review.searchBy}
                options={SEARCH_BY_OPTIONS}
                optionLabel="label"
                optionValue="value"
                onChange={(e) => {
                  if (e.value) onChange({ ...review, searchBy: e.value });
                }}
                allowEmpty={false}
              />
            </div>
            {review.searchBy === SearchByEnum.TEXT && (
              <div
                className="flex flex-column gap-2"
                style={{ flex: "1 1 220px" }}
              >
                <label
                  htmlFor="review-search-text"
                  className="font-medium"
                >
                  The words it waits to read
                </label>
                <InputText
                  id="review-search-text"
                  value={review.searchText}
                  placeholder="Nothing was read here, so type them"
                  onChange={(e) => onChange({ ...review, searchText: e.target.value })}
                  className="w-full"
                />
              </div>
            )}
          </div>
        )}

        {review.choice === ReviewChoiceEnum.INPUT && (
          <div className="flex align-items-end flex-wrap gap-3">
            <div className="flex flex-column gap-2">
              <label
                htmlFor="review-input-name"
                className="font-medium"
              >
                Input name
              </label>
              <InputText
                id="review-input-name"
                value={review.inputName}
                onChange={(e) => onChange({ ...review, inputName: toName(e.target.value) })}
                style={{ width: 200, fontFamily: "ui-monospace, Consolas, monospace" }}
              />
            </div>
            <div className="flex align-items-center gap-2 pb-2">
              <Checkbox
                inputId="review-input-secret"
                checked={review.isSecret}
                onChange={(e) => onChange({ ...review, isSecret: !!e.checked })}
              />
              <label htmlFor="review-input-secret">Secret: never written to a file</label>
            </div>
          </div>
        )}

        {review.choice === ReviewChoiceEnum.TYPE_INPUT && (
          <div className="flex flex-column gap-2">
            <label
              htmlFor="review-typed-input"
              className="font-medium"
            >
              Input name
            </label>
            <InputText
              id="review-typed-input"
              value={review.inputName}
              onChange={(e) => onChange({ ...review, inputName: toName(e.target.value) })}
              style={{ width: 200, fontFamily: "ui-monospace, Consolas, monospace" }}
            />
          </div>
        )}

        {(review.choice === ReviewChoiceEnum.EXECUTE_COMMAND || review.choice === ReviewChoiceEnum.LAUNCH_APP) && (
          <div className="flex flex-column gap-2">
            <label
              htmlFor="review-command"
              className="font-medium"
            >
              {review.choice === ReviewChoiceEnum.EXECUTE_COMMAND ? "Command" : "Program"}
            </label>
            <InputText
              id="review-command"
              value={review.commandText}
              placeholder={review.choice === ReviewChoiceEnum.EXECUTE_COMMAND ? "del /q C:\\Exports\\*.csv" : "notepad.exe, or the program's full path"}
              onChange={(e) => onChange({ ...review, commandText: e.target.value })}
              className="w-full"
              style={{ fontFamily: "ui-monospace, Consolas, monospace" }}
            />
          </div>
        )}

        <p
          className="m-0 text-sm"
          style={MUTED}
        >
          {noteOf(action, review, element?.foundBy === ElementFoundByEnum.FALLBACK ? element.note : null)}
        </p>
      </div>
    </section>
  );
}

function FactComponent({ name, value }: { name: string; value: string }) {
  return (
    <>
      <dt style={MUTED}>{name}</dt>
      <dd className="m-0">{value}</dd>
    </>
  );
}

// Letters, digits and underscores: what a script's input names are.
function toName(text: string): string {
  return text.toLowerCase().replace(/[^a-z0-9_]/g, "_");
}

function factsOf(result: RecordingResultDto, action: RecordedActionDto, review: ActionReview): { name: string; value: string }[] {
  const facts: { name: string; value: string }[] = [];
  const element = elementOf(result, action);

  if (isPointed(action)) {
    let read = "No text: an icon";
    if (!element) read = "Nothing: it was outside the window you picked";
    else if (element.text.length > 0) read = `“${element.text}”`;
    facts.push({ name: "Read under the click", value: read });

    const waited = action.waitedMilliseconds;
    facts.push({ name: "Waited before", value: `${toSeconds(waited)}, so it waits up to ${toSeconds(waitFor(waited))}` });

    const group = groupOf(result, action);
    if (group) facts.push({ name: "Same picture as", value: describeGroup(result, group.actionIndexes, group.isMoving) });
  }

  if (action.kind === RecordedActionKindEnum.TYPING) {
    const text = action.text ?? "";
    // Hidden only while it stays a secret: any other choice writes it into the flow.
    const isHidden = review.choice === ReviewChoiceEnum.INPUT && review.isSecret;
    facts.push({ name: "Typed", value: isHidden ? "•".repeat([...text].length) : text });

    const label = fieldLabelBefore(result, action);
    facts.push({ name: "Into", value: label ? `The field read as “${label}”` : "The field clicked before it" });
  }

  if (action.windowTitle) facts.push({ name: "Window", value: action.windowTitle });

  return facts;
}

function describeGroup(result: RecordingResultDto, actionIndexes: number[], isMoving: boolean): string {
  const listed = listedActions(result);
  const positions = actionIndexes.map((index) => listed.findIndex((x) => x.index === index) + 1);

  let named = `Actions ${positions.slice(0, -1).join(", ")} and ${positions[positions.length - 1]}`;
  const isRun = positions.every((x, i) => i === 0 || x === positions[i - 1] + 1);
  if (isRun && positions.length > 2) named = `Actions ${positions[0]} to ${positions[positions.length - 1]}`;

  return isMoving ? `${named}, each in a different place` : `${named}, in the same place`;
}

function noteOf(action: RecordedActionDto, review: ActionReview, fallbackNote: string | null | undefined): string {
  const waitsUpTo = toSeconds(waitFor(action.waitedMilliseconds));
  const words = review.searchText.trim();

  if (isByWords(review)) {
    switch (review.choice) {
      case ReviewChoiceEnum.WAIT_AND_CLICK:
        return `Becomes three steps: a wait until the area reads “${words}”, a move to the middle of the area, and a click there - so keep the area on what you clicked. Read by Windows, so Windows only for now.`;
      case ReviewChoiceEnum.CHECK_THERE:
        return `Waits until the area reads “${words}”, up to ${waitsUpTo}, and the execution fails if it never does. Read by Windows, so Windows only for now.`;
      case ReviewChoiceEnum.CHECK_GONE:
        return `Waits until the area no longer reads “${words}”, up to ${waitsUpTo}, and the execution fails if it still does. Read by Windows, so Windows only for now.`;
    }
  }

  switch (review.choice) {
    case ReviewChoiceEnum.WAIT_AND_CLICK:
      if (fallbackNote) return `A box around where you clicked: ${fallbackNote}`;
      return "Becomes three steps: a wait for it, a move onto wherever it was found, and a click.";
    case ReviewChoiceEnum.CLICK_AT_SPOT:
      return "Clicks where you clicked, without looking for anything first. It misses when the window moves or the page lays out differently.";
    case ReviewChoiceEnum.INPUT:
      if (review.isSecret) return "A secret input is never written to a file and is never shown to a model.";
      return `The value stays on this machine as the input's default. The flow and the script only see {{${review.inputName}}}.`;
    case ReviewChoiceEnum.TYPE_AS_WRITTEN:
      return "Typed into the flow as it is, so anyone reading the flow reads it too.";
    case ReviewChoiceEnum.WRONG_ON_PURPOSE:
      return "Typed into the flow as it is, never an input, so no row can put it right. The model is told it is wrong on purpose and gives it a stage of its own, which passes when the check you marked after it sees the application refuse it.";
    case ReviewChoiceEnum.CHECK_THERE:
      return `Waits until it is there, up to ${waitsUpTo}, and the execution fails if it never comes. What you did next goes after it.`;
    case ReviewChoiceEnum.CHECK_GONE:
      return `Waits until it has gone, up to ${waitsUpTo}, and the execution fails if it is still there. What you did next goes after it.`;
    case ReviewChoiceEnum.EXECUTE_COMMAND:
      return "Executes the command in cmd and waits for it to finish. Windows only, as every command is for now.";
    case ReviewChoiceEnum.LAUNCH_APP:
      return "Opens the program and carries on without waiting for its window.";
    case ReviewChoiceEnum.TYPE_INPUT:
      return "Types the input's value. One the flow does not have yet is added with no value, to be given before it executes.";
    case ReviewChoiceEnum.LEAVE_OUT:
      if (isOutside(action)) return "Outside the window you picked, so it is left out. Keep it only if the flow really needs it.";
      return "Left out of the flow.";
    default:
      return action.kind === RecordedActionKindEnum.SCROLL ? "Becomes a Scroll step." : "Becomes a Press step.";
  }
}
