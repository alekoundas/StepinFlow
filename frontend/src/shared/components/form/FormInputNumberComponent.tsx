import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { FlowDto } from "@/shared/models/database/flow/flow-dto";
import { InputNumber } from "primereact/inputnumber";
import { classNames } from "primereact/utils";
import { useController, useFormContext, type Control } from "react-hook-form";

interface Props {
  fieldName: keyof FlowStepDto | keyof FlowDto;
  label: string;
  placeholderText?: string;
  hintText?: string;
  min?: number;
  max?: number;
  isDisabled?: boolean;
  isRequired?: boolean;
}

export function FormInputNumberComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
  min,
  max = 2147483647, // signed int32 Max
  isDisabled = false,
  isRequired = false,
}: Props) {
  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController({ name: fieldName });
  return (
    <>
      <div className="field">
        <LabelComponent
          text={label}
          weight="bold"
        />
        <InputNumber
          ref={ref}
          name={fieldName}
          value={value}
          onChange={(e) => onChange(e.value)}
          onBlur={onBlur}
          placeholder={placeholderText}
          min={min}
          max={max}
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
