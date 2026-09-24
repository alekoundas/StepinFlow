import { classNames } from "primereact/utils";
import { SelectButton } from "primereact/selectbutton";
import { useController } from "react-hook-form";

import LabelComponent from "@/shared/components/LabelComponent";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";

// Null is stored as null. This only names it so the button for it can be selected.
const SAME_AS_PARENT = "SAME_AS_PARENT";

interface Props {
  type: FlowAreaTypeEnum;
  // Null when it sits inside nothing.
  parentName: string | null;
  isDisabled?: boolean;
}

const HINTS: Record<string, string> = {
  [ScalesWithEnum.DPI]:
    "Its contents keep their size when the window changes, like a browser or a normal app. Only the monitor's scaling makes them bigger.",
  [ScalesWithEnum.AREA]:
    "Its contents stretch with the area, like a game. Templates grow and shrink with its size.",
};

/**
 * What makes the things inside this area bigger or smaller on another screen.
 *
 * Null means "the default": a region inside another follows it, and anything else follows the
 * DPI. An application is the one type with no default - a native app and a game look the same
 * from outside - so it shows nothing picked until someone says which.
 */
export default function FlowAreaScalesWithFieldComponent({
  type,
  parentName,
  isDisabled = false,
}: Props) {
  const {
    field: { value, onChange, onBlur },
    fieldState: { error },
  } = useController({ name: "scalesWith" });

  const isInside = type === FlowAreaTypeEnum.CUSTOM && parentName !== null;

  const options = [
    { label: "Screen DPI", value: ScalesWithEnum.DPI },
    { label: "Area size", value: ScalesWithEnum.AREA },
  ];
  if (isInside)
    options.unshift({
      label: `Same as "${parentName}"`,
      value: SAME_AS_PARENT as ScalesWithEnum,
    });

  let shown: string | null = value ?? null;
  if (shown === null && isInside) shown = SAME_AS_PARENT;
  else if (shown === null && type !== FlowAreaTypeEnum.APPLICATION)
    shown = ScalesWithEnum.DPI;

  let hint =
    "A browser or a normal app keeps its contents' size when resized; a game stretches them. Pick which this is.";
  if (shown === SAME_AS_PARENT) hint = `Follows "${parentName}".`;
  else if (shown !== null) hint = HINTS[shown];

  return (
    <div className="field">
      <LabelComponent
        text="Contents scale with"
        weight="bold"
        isRequired={type === FlowAreaTypeEnum.APPLICATION}
      />

      <SelectButton
        value={shown}
        options={options}
        optionLabel="label"
        optionValue="value"
        disabled={isDisabled}
        onBlur={onBlur}
        onChange={(e) => {
          if (e.value === null) return;
          onChange(e.value === SAME_AS_PARENT ? null : e.value);
        }}
        className={classNames({ "p-invalid": error !== undefined })}
      />

      <LabelComponent text={hint} weight="bold" size="xs" className="mt-1" />
      <LabelComponent
        text={error?.message ?? ""}
        weight="normal"
        size="sm"
        hidden={error?.message === undefined}
        color="error"
        className="mt-1"
      />
    </div>
  );
}
