import LabelComponent from "@/shared/components/LabelComponent";

import { classNames } from "primereact/utils";
import { useController } from "react-hook-form";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import { Checkbox, type CheckboxChangeEvent } from "primereact/checkbox";

interface Props {
  fieldName: keyof FlowStepDto | keyof FlowDto;
  label: string;
  placeholderText?: string;
  hintText?: string;
  isDisabled?: boolean;
  isRequired?: boolean;

  // Actions
  onChanged?: (value: boolean) => void;
}

export function FormInputCheckboxComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
  isDisabled = false,
  isRequired = false,

  onChanged,
}: Props) {
  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController({ name: fieldName });

  const handleChange = (value: boolean | undefined): void => {
    onChange(value); // Call ReacHookForm onChange
    if (onChanged) {
      onChanged(!!value); // Call parent onChanged
    }
  };

  return (
    <>
      <div className="field">
        <LabelComponent
          text={label}
          weight="bold"
        />
        <Checkbox
          ref={ref}
          name={fieldName}
          checked={value}
          onChange={(e: CheckboxChangeEvent) => handleChange(e.checked)}
          onBlur={onBlur}
          placeholder={placeholderText}
          disabled={isDisabled}
          required={isRequired}
          className={classNames("w-full", { "p-invalid": invalid })}
        />
        <LabelComponent
          text={hintText ?? ""}
          weight="bold"
          size="xs"
          hidden={hintText === undefined}
        />
        <LabelComponent
          text={error?.message ?? ""}
          weight="normal"
          size="sm"
          hidden={error?.message === undefined}
          className="p-error mt-1 block"
        />
      </div>
    </>
  );
}
