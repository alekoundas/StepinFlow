import { useState } from "react";
import { Dropdown } from "primereact/dropdown";
import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";

import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import DiscordBotPickerComponent from "@/features/recording/gesture/DiscordBotPickerComponent";
import { type GestureFields, toScriptName, WAIT_UNITS } from "@/features/recording/gesture/gesture";

const MONOSPACE = { fontFamily: "ui-monospace, Consolas, monospace" };

interface Props {
  fields: GestureFields;
  onChange: (patch: Partial<GestureFields>) => void;
  onNewBot: () => void;
}

/** What a step marked instead of a click needs, by which step it is. */
export default function StepFieldsComponent({ fields, onChange, onNewBot }: Props) {
  switch (fields.choice) {
    case ReviewChoiceEnum.EXECUTE_COMMAND:
      return (
        <FieldComponent
          id="gesture-command"
          label="Command"
        >
          <InputTextarea
            id="gesture-command"
            value={fields.commandText}
            rows={3}
            autoResize
            placeholder="del /q C:\Exports\*.csv"
            onChange={(e) => onChange({ commandText: e.target.value })}
            className="w-full"
            style={MONOSPACE}
          />
        </FieldComponent>
      );

    case ReviewChoiceEnum.LAUNCH_APP:
      return (
        <FieldComponent
          id="gesture-program"
          label="Program"
        >
          <InputText
            id="gesture-program"
            value={fields.commandText}
            placeholder="notepad.exe, or the program's full path"
            onChange={(e) => onChange({ commandText: e.target.value })}
            className="w-full"
            style={MONOSPACE}
          />
        </FieldComponent>
      );

    case ReviewChoiceEnum.TYPE_INPUT:
      return (
        <FieldComponent
          id="gesture-input"
          label="Input name"
        >
          <InputText
            id="gesture-input"
            value={fields.inputName}
            placeholder="email"
            onChange={(e) => onChange({ inputName: toScriptName(e.target.value) })}
            style={{ ...MONOSPACE, width: 240 }}
          />
        </FieldComponent>
      );

    case ReviewChoiceEnum.NOTIFY:
      return (
        <div className="flex flex-column gap-3">
          <FieldComponent
            id="gesture-step-bot"
            label="Send through"
          >
            <DiscordBotPickerComponent
              inputId="gesture-step-bot"
              value={fields.discordBotId}
              onChange={(discordBotId) => onChange({ discordBotId })}
              onNew={onNewBot}
            />
            {fields.discordBotId === null && (
              <small className="p-error">A notification needs a bot to send through. Pick one, or add a new one.</small>
            )}
          </FieldComponent>
          <FieldComponent
            id="gesture-step-message"
            label="Message"
          >
            <InputTextarea
              id="gesture-step-message"
              value={fields.message}
              rows={3}
              autoResize
              maxLength={1500}
              placeholder="An order went through. Check the stock sheet."
              onChange={(e) => onChange({ message: e.target.value })}
              className="w-full"
            />
          </FieldComponent>
        </div>
      );

    case ReviewChoiceEnum.WAIT:
      return (
        <WaitFieldsComponent
          waitMilliseconds={fields.waitMilliseconds}
          onChange={(waitMilliseconds) => onChange({ waitMilliseconds })}
        />
      );

    case ReviewChoiceEnum.BEGIN_STAGE:
      return (
        <FieldComponent
          id="gesture-stage"
          label="Stage name"
        >
          <InputText
            id="gesture-stage"
            value={fields.stageName}
            placeholder="What this part tests, such as Delete customer"
            onChange={(e) => onChange({ stageName: e.target.value })}
            className="w-full"
          />
        </FieldComponent>
      );

    default:
      return null;
  }
}

function FieldComponent({ id, label, children }: { id: string; label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-column gap-2">
      <label
        htmlFor={id}
        className="font-medium"
      >
        {label}
      </label>
      {children}
    </div>
  );
}

// The amount in the unit it divides into evenly, largest first: 30 minutes rather than 1800 seconds.
function WaitFieldsComponent({ waitMilliseconds, onChange }: { waitMilliseconds: number; onChange: (milliseconds: number) => void }) {
  const [unit, setUnit] = useState(() => {
    const fitting = [...WAIT_UNITS].reverse().find((x) => waitMilliseconds > 0 && waitMilliseconds % x.value === 0);
    return fitting?.value ?? WAIT_UNITS[0].value;
  });

  const amount = Math.round(waitMilliseconds / unit);

  return (
    <FieldComponent
      id="gesture-wait"
      label="Wait for"
    >
      <div className="flex align-items-center gap-2">
        <InputNumber
          inputId="gesture-wait"
          value={amount}
          min={1}
          useGrouping={false}
          onValueChange={(e) => onChange((e.value ?? 0) * unit)}
          inputStyle={{ width: 120 }}
        />
        <Dropdown
          aria-label="Unit"
          value={unit}
          options={WAIT_UNITS}
          optionLabel="label"
          optionValue="value"
          onChange={(e) => {
            setUnit(e.value);
            onChange(amount * e.value);
          }}
          style={{ width: 150 }}
        />
      </div>
    </FieldComponent>
  );
}
