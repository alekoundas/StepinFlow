import LabelComponent from "@/shared/components/LabelComponent";
import type { FormFieldName } from "@/shared/models/form-field-name";
import {
  InputNumber,
  type InputNumberChangeEvent,
} from "primereact/inputnumber";
import { classNames } from "primereact/utils";
import { useController } from "react-hook-form";

interface Props {
  fieldName: FormFieldName;
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

/**
 * Whole numbers only: pixels, counts, milliseconds. The decimal point is not accepted, so a
 * value the int schemas would reject can never be typed. Floats belong in
 * FormInputFloatComponent.
 */
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

  const handleChange = (nextValue: number | null): void => {
    // The input cannot produce a fraction, but a rounded value still beats passing whatever
    // arrived straight to a schema that demands an integer.
    const whole = nextValue == null ? null : Math.round(nextValue);

    // Clearing a required field means 0, so the schema still sees a number. An optional one is
    // allowed to be empty -- but a typed value has to survive, which is what used to be dropped.
    const cleanedValue = whole ?? (isRequired ? 0 : null);

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
          maxFractionDigits={0}
          disabled={isDisabled}
          // required={isRequired}
          className={classNames("w-full", { "p-invalid": invalid })}
          // The input inside keeps a default width of its own, which overflows a narrow column.
          inputClassName="w-full min-w-0"
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
