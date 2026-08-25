import { useState } from "react";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepWaitFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue, getValues } = useFormContext();

  const min = useWatch({ control, name: "waitForMilliseconds" }) as number | undefined;
  const max = useWatch({ control, name: "waitForMillisecondsMax" }) as number | undefined;

  // Zero is how "not a range" is stored, so the checkbox is derived rather than saved.
  const [isRandom, setIsRandom] = useState<boolean>((max ?? 0) > 0);

  const toggleRandom = (value: boolean) => {
    setIsRandom(value);

    setValue(
      "waitForMillisecondsMax",
      // Something sensible to start from rather than an error the moment it is ticked.
      value ? Math.max(((getValues("waitForMilliseconds") as number) ?? 1000) * 2, 100) : 0,
      { shouldValidate: true, shouldDirty: true },
    );
  };

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormInputNumberComponent
        fieldName="waitForMilliseconds"
        label={isRandom ? "Shortest wait (ms)" : "Wait duration (ms)"}
        hintText={humanize(min)}
        min={50}
        max={2147483647} // signed int32 Max
        isRequired={true}
        isDisabled={isDisabled}
      />

      <div className="flex flex-column gap-1 mt-2">
        <div className="flex align-items-center gap-2">
          <input
            id="wait-random"
            type="checkbox"
            checked={isRandom}
            disabled={isDisabled}
            onChange={(e) => toggleRandom(e.target.checked)}
          />
          <label htmlFor="wait-random">
            <LabelComponent text="Wait a random amount instead" />
          </label>
        </div>

        <LabelComponent
          text="A step that pauses for exactly the same time every run leaves a pattern. Picks evenly between the two values."
          size="xs"
          color="secondary"
        />
      </div>

      {isRandom && (
        <>
          <FormInputNumberComponent
            fieldName="waitForMillisecondsMax"
            label="Longest wait (ms)"
            hintText={humanize(max)}
            min={50}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
          />

          {min != null && max != null && max > min && (
            <LabelComponent
              text={`Each run waits somewhere between ${humanize(min)} and ${humanize(max)}.`}
              size="sm"
              color="secondary"
            />
          )}
        </>
      )}
    </>
  );
}

/** Milliseconds are what gets stored; nobody reads 5400000 and thinks "an hour and a half". */
const humanize = (value: number | undefined | null): string => {
  if (!value || value <= 0) return "";

  const units: [number, string][] = [
    [86_400_000, "d"],
    [3_600_000, "h"],
    [60_000, "m"],
    [1_000, "s"],
  ];

  let remaining = value;
  const parts: string[] = [];

  for (const [size, suffix] of units) {
    const count = Math.floor(remaining / size);
    if (count > 0) {
      parts.push(`${count}${suffix}`);
      remaining -= count * size;
    }
  }

  if (remaining > 0) parts.push(`${remaining}ms`);

  return parts.join(" ");
};
