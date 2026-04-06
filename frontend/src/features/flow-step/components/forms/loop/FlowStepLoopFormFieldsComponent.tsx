import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { classNames } from "primereact/utils";

interface Props {
  isDisabled?: boolean;
  className?: string;
}

export default function FlowStepLoopFormFieldsComponent({
  isDisabled = false,
  className,
}: Props) {
  const { control, setValue, trigger } = useFormContext();
  
  // Watch the two fields that control each other
  const isLoopInfinite = useWatch({ control, name: "isLoopInfinite" });
  const loopCount = useWatch({ control, name: "loopCount" });

  const isInfiniteActive = isLoopInfinite === true;
  const isFiniteActive = (loopCount ?? 0) > 0;

  // Main logic: enforce mutual exclusivity + force revalidation of both
  useEffect(() => {
    if (isInfiniteActive && loopCount !== 0) {
      setValue("loopCount", 0, { shouldValidate: false, shouldDirty: true });
    }

    if (isFiniteActive && isLoopInfinite === true) {
      setValue("isLoopInfinite", false, {
        shouldValidate: false,
        shouldDirty: true,
      });
    }

    // Always re-validate BOTH fields after any change
    trigger(["isLoopInfinite", "loopCount"]);

    // Optional: if the state is now valid, aggressively clear old cross errors
    // if (isInfiniteActive || isFiniteActive) {
    //   clearErrors(["isLoopInfinite", "loopCount"]);
    // }
  }, [isLoopInfinite, loopCount, setValue, trigger]);

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
      <div className={classNames(className, "flex", "flex-column")}>
        <FormInputTextComponent
          fieldName="name"
          label="Name"
          isRequired={isDisabled ? false : true}
          isDisabled={isDisabled}
        />

        <FormInputNumberComponent
          fieldName="loopCount"
          label="Loop Count"
          min={0}
          max={2147483647}
          isRequired={isDisabled || disableLoopCountInput ? false : true}
          isDisabled={isDisabled || disableLoopCountInput}
          hintText={isDisabled ? undefined : loopCountHint}
        />

        <FormInputCheckboxComponent
          fieldName="isLoopInfinite"
          label="Infinite Loop"
          isRequired={isDisabled || disableInfiniteCheckbox ? false : true}
          isDisabled={isDisabled || disableInfiniteCheckbox}
          hintText={isDisabled ? undefined : infiniteHint}
        />
      </div>
    </>
  );
}
