import type { CSSProperties } from "react";
import { Checkbox } from "primereact/checkbox";

import { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { RecordingResultDto } from "@/shared/models/recording-result-dto";
import {
  type ActionReview,
  elementOf,
  groupOf,
  isOutside,
  isPointed,
  listedActions,
  startsPage,
  subtitleOf,
  titleOf,
} from "@/features/recording/review/review";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  result: RecordingResultDto;
  reviews: Record<number, ActionReview>;
  selectedIndex: number;
  onSelect: (actionIndex: number) => void;
  onToggle: (action: RecordedActionDto) => void;
}

/** Every action, numbered, kept or left out with its checkbox, and the one shown beside it marked. */
export default function ReviewActionListComponent({ result, reviews, selectedIndex, onSelect, onToggle }: Props) {
  const listed = listedActions(result);
  const leftOut = listed.filter((x) => reviews[x.index]?.choice === ReviewChoiceEnum.LEAVE_OUT).length;

  return (
    <section
      aria-labelledby="review-actions-heading"
      className="border-1 border-round-xl overflow-hidden"
      style={{ flex: "1 1 360px", minWidth: 0, background: "var(--surface-card)", borderColor: "var(--surface-border)" }}
    >
      <div
        className="flex align-items-center justify-content-between px-3 py-3 border-bottom-1"
        style={{ borderColor: "var(--surface-border)" }}
      >
        <h2
          id="review-actions-heading"
          className="m-0 text-sm font-semibold"
        >
          Actions
        </h2>
        <span
          className="text-xs"
          style={MUTED}
        >
          {listed.length} recorded · {leftOut} left out
        </span>
      </div>

      <ol className="list-none m-0 p-2 flex flex-column gap-1">
        {listed.map((action, position) => {
          const review = reviews[action.index];
          const element = elementOf(result, action);
          const group = groupOf(result, action);
          const isKept = review.choice !== ReviewChoiceEnum.LEAVE_OUT;
          const isSelected = action.index === selectedIndex;
          const stage = action.kind === RecordedActionKindEnum.STAGE ? "" : review.stageName.trim();

          return (
            <li
              key={action.index}
              className="flex align-items-center gap-2 py-1 pl-2 pr-1 border-round-lg"
              style={{
                background: isSelected ? "var(--surface-hover)" : undefined,
                opacity: isKept ? 1 : 0.5,
              }}
            >
              <Checkbox
                inputId={`review-keep-${action.index}`}
                checked={isKept}
                onChange={() => onToggle(action)}
                aria-label={`Keep action ${position + 1}`}
              />
              <button
                type="button"
                aria-pressed={isSelected}
                onClick={() => onSelect(action.index)}
                className="flex flex-1 align-items-center gap-2 py-1 px-1 border-none cursor-pointer text-left min-w-0"
                style={{ background: "transparent", color: "inherit", font: "inherit" }}
              >
                <span
                  className="text-xs text-right flex-none"
                  style={{ ...MUTED, width: 18, fontVariantNumeric: "tabular-nums" }}
                >
                  {position + 1}
                </span>
                <span
                  aria-hidden="true"
                  className="flex align-items-center justify-content-center border-round-lg flex-none"
                  style={{ width: 26, height: 26, background: "var(--surface-ground)", color: tintOf(action) }}
                >
                  <i
                    className={iconOf(action)}
                    style={{ fontSize: 12 }}
                  />
                </span>
                <span className="flex flex-column flex-1 min-w-0">
                  <span className="text-sm font-medium white-space-nowrap overflow-hidden text-overflow-ellipsis">
                    {titleOf(action, element)}
                  </span>
                  <span
                    className="text-xs white-space-nowrap overflow-hidden text-overflow-ellipsis"
                    style={MUTED}
                  >
                    {subtitleOf(action, review)}
                  </span>
                </span>
                {stage && (
                  <span
                    className="text-xs border-1 white-space-nowrap overflow-hidden text-overflow-ellipsis flex-none px-2"
                    style={{ color: "var(--purple-300)", borderColor: "var(--purple-300)", borderRadius: 999, maxWidth: 140 }}
                  >
                    ## {stage}
                  </span>
                )}
                {!stage && action.kind !== RecordedActionKindEnum.STAGE && startsPage(result, action) && (
                  <span
                    className="text-xs border-1 white-space-nowrap flex-none px-2"
                    style={{ ...MUTED, borderColor: "var(--surface-border)", borderRadius: 999 }}
                  >
                    new page
                  </span>
                )}
                {group && (
                  <span
                    className="text-xs border-1 white-space-nowrap flex-none px-2"
                    style={{ color: "var(--primary-color)", borderColor: "var(--primary-color)", borderRadius: 999 }}
                  >
                    same target ×{group.actionIndexes.length}
                  </span>
                )}
                {element && (
                  <img
                    src={`data:image/png;base64,${element.image}`}
                    alt=""
                    className="flex-none border-round-md"
                    style={{ height: 24, maxWidth: 86, objectFit: "contain", background: "#F6F7F9" }}
                  />
                )}
              </button>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

function iconOf(action: RecordedActionDto): string {
  if (isOutside(action)) return "pi pi-ban";

  switch (action.kind) {
    case RecordedActionKindEnum.CLICK:
      return "pi pi-arrow-up-left";
    case RecordedActionKindEnum.DRAG:
      return "pi pi-arrows-alt";
    case RecordedActionKindEnum.SCROLL:
      return "pi pi-sort-alt";
    case RecordedActionKindEnum.TYPING:
      return "pi pi-pencil";
    case RecordedActionKindEnum.CHECK:
      return "pi pi-check-circle";
    case RecordedActionKindEnum.SUBSTITUTE:
      return "pi pi-bolt";
    case RecordedActionKindEnum.STAGE:
      return "pi pi-flag";
    default:
      return "pi pi-th-large";
  }
}

function tintOf(action: RecordedActionDto): string {
  if (isOutside(action)) return "var(--text-color-secondary)";
  if (isPointed(action)) return "var(--primary-color)";

  return "var(--purple-300)";
}
