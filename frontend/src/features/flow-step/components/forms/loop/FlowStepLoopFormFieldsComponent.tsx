import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepLoopFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  // Watch the two fields that control each other
  const isLoopInfinite: boolean | undefined = useWatch({
    control,
    name: "isLoopInfinite",
  });
  const loopCount: number | undefined = useWatch({
    control,
    name: "loopCount",
  });

  const isInfiniteActive = isLoopInfinite === true;
  const isFiniteActive = (loopCount ?? 0) > 0;

  // When user enables Infinite → clear loopCount and vice versa
  useEffect(() => {
    if (isInfiniteActive && loopCount !== 0) {
      setValue("loopCount", 0, { shouldValidate: true });
    }
  }, [isInfiniteActive, loopCount, setValue]);

  useEffect(() => {
    if (isFiniteActive && isLoopInfinite) {
      setValue("isLoopInfinite", false, { shouldValidate: true });
    }
  }, [isFiniteActive, isLoopInfinite, setValue]);

  // Disable logic
  const disableInfiniteCheckbox = isFiniteActive;
  const disableLoopCountInput = isInfiniteActive;

  // Hint texts
  const loopCountHint = disableLoopCountInput
    ? "Disabled because Infinite Loop is enabled"
    : undefined;

  const infiniteHint = disableInfiniteCheckbox
    ? "Disabled because Loop Count is set > 0"
    : undefined;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormInputNumberComponent
        fieldName="loopCount"
        label="Loop Count"
        min={0}
        max={2147483647}
        isRequired={true}
        isDisabled={isDisabled || disableLoopCountInput}
        hintText={loopCountHint}
      />

      <FormInputCheckboxComponent
        fieldName="isLoopInfinite"
        label="Infinite Loop"
        isRequired={false}
        isDisabled={isDisabled || disableInfiniteCheckbox}
        hintText={infiniteHint}
      />
    </>
  );
}
