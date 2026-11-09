import { InputSwitch } from "primereact/inputswitch";
import { InputTextarea } from "primereact/inputtextarea";

import { SearchByEnum } from "@/shared/enums/backend/search-by-enum";
import DiscordBotPickerComponent from "@/features/recording/gesture/DiscordBotPickerComponent";
import { type GestureFields, notifyLabels } from "@/features/recording/gesture/gesture";

interface Props {
  fields: GestureFields;
  onChange: (patch: Partial<GestureFields>) => void;
  onNewBot: () => void;
}

/**
 * What a check tells Discord: when it succeeds, when it fails, or both, each with its own message,
 * through one bot. The check's result goes with each, so there is nothing to pick for it.
 */
export default function CheckNotifyFieldsComponent({ fields, onChange, onNewBot }: Props) {
  const labels = notifyLabels(fields.choice);
  const isNotifying = fields.notifyOnSuccess || fields.notifyOnFailure;

  let found = "what it found and how close it came";
  if (fields.searchBy === SearchByEnum.TEXT) found = "the words it read";

  return (
    <div
      className="flex flex-column gap-3 p-3 border-1 border-round-lg"
      style={{ borderColor: "var(--surface-border)", background: "var(--surface-ground)" }}
    >
      <div className="flex align-items-center justify-content-between flex-wrap gap-3">
        <span className="flex align-items-center gap-2 font-medium">
          <i
            className="pi pi-bell text-color-secondary"
            aria-hidden="true"
          />
          Tell me on Discord
        </span>

        <div className="flex align-items-center flex-wrap gap-4">
          <SwitchComponent
            inputId="gesture-notify-success"
            label={labels.success}
            checked={fields.notifyOnSuccess}
            onChange={(checked) => onChange({ notifyOnSuccess: checked })}
          />
          <SwitchComponent
            inputId="gesture-notify-failure"
            label={labels.failure}
            checked={fields.notifyOnFailure}
            onChange={(checked) => onChange({ notifyOnFailure: checked })}
          />
        </div>
      </div>

      {isNotifying && (
        <div className="flex flex-column gap-2">
          <div className="flex align-items-center gap-3">
            <label
              htmlFor="gesture-notify-bot"
              className="text-sm text-color-secondary flex-none"
              style={{ width: 96 }}
            >
              Send through
            </label>
            <DiscordBotPickerComponent
              inputId="gesture-notify-bot"
              value={fields.discordBotId}
              onChange={(discordBotId) => onChange({ discordBotId })}
              onNew={onNewBot}
            />
          </div>
          {fields.discordBotId === null && (
            <small className="p-error">A notification needs a bot to send through. Pick one, or add a new one.</small>
          )}
        </div>
      )}

      {fields.notifyOnSuccess && (
        <MessageComponent
          id="gesture-notify-success-message"
          label={`Message ${labels.success.toLowerCase()}`}
          value={fields.successMessage}
          placeholder="Optional, such as: the tote is in the cart."
          hint={`Sent under ${found}, then the flow carries on with what you did next.`}
          onChange={(successMessage) => onChange({ successMessage })}
        />
      )}

      {fields.notifyOnFailure && (
        <MessageComponent
          id="gesture-notify-failure-message"
          label={`Message ${labels.failure.toLowerCase()}`}
          value={fields.failureMessage}
          placeholder="Optional, such as: Remove never showed, look at the shop."
          hint="Sent with why it failed, then the execution ends as failed."
          onChange={(failureMessage) => onChange({ failureMessage })}
        />
      )}
    </div>
  );
}

function SwitchComponent({
  inputId,
  label,
  checked,
  onChange,
}: {
  inputId: string;
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <span className="flex align-items-center gap-2">
      <InputSwitch
        inputId={inputId}
        checked={checked}
        onChange={(e) => onChange(!!e.value)}
      />
      <label
        htmlFor={inputId}
        className="text-sm cursor-pointer"
      >
        {label}
      </label>
    </span>
  );
}

function MessageComponent({
  id,
  label,
  value,
  placeholder,
  hint,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  hint: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="flex flex-column gap-2">
      <label
        htmlFor={id}
        className="text-sm font-medium"
      >
        {label}
      </label>
      <InputTextarea
        id={id}
        value={value}
        rows={2}
        autoResize
        maxLength={1500}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full"
      />
      <span className="text-xs text-color-secondary">{hint}</span>
    </div>
  );
}
