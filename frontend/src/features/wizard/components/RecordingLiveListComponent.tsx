import { classNames } from "primereact/utils";

import { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import type { RecordingLiveActionDto } from "@/shared/models/recording-live-action-dto";
import { toClock } from "@/shared/utils/clock";

interface Props {
  actions: RecordingLiveActionDto[];
}

/**
 * The actions as they fold while recording - "Typed 15 characters" rather than every key-down and
 * key-up - with the newest last and marked. Shown on the recording bar and the recording page.
 */
export default function RecordingLiveListComponent({ actions }: Props) {
  return (
    <div className="flex flex-column gap-1">
      {actions.map((action, position) => (
        <div
          key={action.index}
          className={classNames(
            "flex align-items-center gap-2 px-2 py-1 border-round",
            position === actions.length - 1 && "surface-hover",
          )}
        >
          <span
            className="flex align-items-center justify-content-center flex-shrink-0 border-round surface-ground text-xs font-bold"
            style={{ width: 24, height: 24, color: tintOf(action.kind) }}
          >
            {glyphOf(action.kind)}
          </span>

          <span className="flex-1 min-w-0 text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {action.line}
          </span>

          <span
            className="text-xs text-color-secondary"
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            {toClock(action.elapsedMilliseconds)}
          </span>
        </div>
      ))}
    </div>
  );
}

const glyphOf = (kind: RecordedActionKindEnum): string => {
  switch (kind) {
    case RecordedActionKindEnum.CLICK:
      return "C";
    case RecordedActionKindEnum.DRAG:
      return "D";
    case RecordedActionKindEnum.SCROLL:
      return "S";
    case RecordedActionKindEnum.TYPING:
      return "T";
    default:
      return "K";
  }
};

// The pointer in one colour, the keyboard in another.
const tintOf = (kind: RecordedActionKindEnum): string => {
  switch (kind) {
    case RecordedActionKindEnum.CLICK:
    case RecordedActionKindEnum.DRAG:
    case RecordedActionKindEnum.SCROLL:
      return "var(--blue-300)";
    default:
      return "var(--purple-300)";
  }
};
