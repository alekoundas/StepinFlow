import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import {
  InputNumber,
  type InputNumberChangeEvent,
} from "primereact/inputnumber";
import { classNames } from "primereact/utils";
import { useController } from "react-hook-form";

interface Props {
  fieldName: keyof FlowStepDto | keyof FlowDto | keyof FlowSearchAreaDto;
  label: string;
  placeholderText?: string;
  hintText?: string;
  min?: number;
  max?: number;
  isDisabled?: boolean;
  isRequired?: boolean;
  className?: string;
  // Actions
  onChanged?: (value: number | null) => void;
}

export function FormInputNumberComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
  min,
  max,
  isDisabled = false,
  isRequired = false,
  className,
  onChanged,
}: Props) {
  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController({ name: fieldName });

  const handleChange = (value: number | null): void => {
    const cleanedValue = isRequired ? (value ?? 0) : null;

    onChange(cleanedValue); // Call ReacHookForm onChange
    if (onChanged) {
      onChanged(cleanedValue); // Call parent onChanged
    }
  };

  return (
    <>
      <div className={classNames("field", className)}>
        <LabelComponent
          text={label}
          weight="bold"
          isRequired={isRequired}
        />
        <InputNumber
          ref={ref}
          name={fieldName}
          value={value}
          onChange={(e: InputNumberChangeEvent) => handleChange(e.value)}
          onBlur={onBlur}
          placeholder={placeholderText}
          min={min}
          max={max}
          disabled={isDisabled}
          // required={isRequired}
          className={classNames("w-full", { "p-invalid": invalid })}
        />
        <LabelComponent
          text={hintText ?? ""}
          weight="bold"
          size="xs"
          hidden={hintText === undefined}
          className="mt-1"
        />
        <LabelComponent
          text={error?.message ?? ""}
          weight="normal"
          size="sm"
          hidden={error?.message === undefined}
          color="error"
          className="mt-1"
        />
      </div>
    </>
  );
}
