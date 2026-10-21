import { useState } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { SelectButton } from "primereact/selectbutton";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { humanizeMilliseconds } from "@/shared/utils/humanize-milliseconds";

interface Props {
  isDisabled?: boolean;
}

const HOW_LONG_OPTIONS = [
  { label: "Fixed", value: false },
  { label: "Random range", value: true },
];

export default function FlowStepWaitFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue, getValues } = useFormContext();

  const min = useWatch({ control, name: "waitForMilliseconds" }) as number | undefined;
  const max = useWatch({ control, name: "waitForMillisecondsMax" }) as number | undefined;

  // Zero is how "not a range" is stored, so the choice is derived rather than saved.
  const [isRandom, setIsRandom] = useState<boolean>((max ?? 0) > 0);

  const toggleRandom = (value: boolean) => {
    setIsRandom(value);

    setValue(
      "waitForMillisecondsMax",
      // Something sensible to start from rather than an error the moment it is picked.
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

      <div className="field">
        <LabelComponent
          text="How long"
          weight="bold"
        />
        <SelectButton
          value={isRandom}
          options={HOW_LONG_OPTIONS}
          disabled={isDisabled}
          onChange={(e) => (e.value !== null ? toggleRandom(e.value) : null)}
        />
        <LabelComponent
          text={
            isRandom
              ? "A step that pauses for exactly the same time every execution leaves a pattern. Picks evenly between the two."
              : "A last resort: a Wait For check waits only as long as it has to."
          }
          size="xs"
          weight="bold"
          className="mt-1"
        />
      </div>

      <div className="flex gap-3">
        <FormInputNumberComponent
          fieldName="waitForMilliseconds"
          label={isRandom ? "Shortest (ms)" : "Wait (ms)"}
          hintText={humanizeMilliseconds(min)}
          min={50}
          max={2147483647} // signed int32 Max
          isRequired={true}
          isDisabled={isDisabled}
          className="flex-1"
        />

        {isRandom && (
          <FormInputNumberComponent
            fieldName="waitForMillisecondsMax"
            label="Longest (ms)"
            hintText={humanizeMilliseconds(max)}
            min={50}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
            className="flex-1"
          />
        )}
      </div>
    </>
  );
}
