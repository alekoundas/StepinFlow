import { useFormContext, useWatch } from "react-hook-form";
import { SelectButton } from "primereact/selectbutton";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  isDisabled?: boolean;
}

const REPEAT_OPTIONS = [
  { label: "A number of times", value: false },
  { label: "Forever", value: true },
];

export default function FlowStepLoopFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue, getValues, trigger } = useFormContext();
  const isLoopInfinite = useWatch({ control, name: "isLoopInfinite" }) === true;

  // One or the other: forever stores no count, and a count is never forever. Both are checked
  // again together, since the rule that joins them puts its error on both.
  const pick = (isForever: boolean) => {
    const count = getValues("loopCount") as number | undefined;

    setValue("isLoopInfinite", isForever, { shouldDirty: true });
    setValue("loopCount", isForever ? 0 : Math.max(count ?? 0, 1), { shouldDirty: true });
    void trigger(["isLoopInfinite", "loopCount"]);
  };

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={!isDisabled}
        isDisabled={isDisabled}
      />

      <div className="field">
        <LabelComponent
          text="Repeat"
          weight="bold"
        />
        <SelectButton
          value={isLoopInfinite}
          options={REPEAT_OPTIONS}
          disabled={isDisabled}
          onChange={(e) => (e.value !== null ? pick(e.value) : null)}
        />
        {isLoopInfinite && (
          <LabelComponent
            text="Ended by a Break or an End Execution inside it."
            size="xs"
            weight="bold"
            className="mt-1"
          />
        )}
      </div>

      {!isLoopInfinite && (
        <FormInputNumberComponent
          fieldName="loopCount"
          label="Times"
          min={1}
          max={2147483647}
          isRequired={!isDisabled}
          isDisabled={isDisabled}
        />
      )}
    </>
  );
}
