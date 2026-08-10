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
  // Ratios are stored 0..1 but nobody reads them that way, so the field shows 0..100 with a
  // % suffix and converts on the way in and out. Storage stays normalized.
  isPercent?: boolean;
  // Digits kept after the point, counted in the unit on screen.
  decimals?: number;
  className?: string;
  // Actions
  onChanged?: (value: number | null) => void;
}

const roundTo = (value: number, decimals: number): number => {
  const factor = 10 ** decimals;
  return Math.round(value * factor) / factor;
};

/**
 * Decimal counterpart to FormInputNumberComponent, for fields the backend holds as float:
 * ratios, tolerances, thresholds. Whole numbers belong in the integer input, which refuses a
 * decimal point outright rather than letting one through to a schema that rejects it.
 */
export function FormInputFloatComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
  min,
  max,
  isDisabled = false,
  isRequired = false,
  isPercent = false,
  decimals = 2,
  className,
  onChanged,
}: Props) {
  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController({ name: fieldName });

  // Percent is a display unit only. Storing to the same precision the field shows keeps the
  // round trip stable instead of accumulating float noise on every edit.
  const scale = isPercent ? 100 : 1;
  const storedDecimals = isPercent ? decimals + 2 : decimals;

  const displayValue =
    typeof value === "number" ? roundTo(value * scale, decimals) : value;

  const handleChange = (nextValue: number | null): void => {
    const stored =
      nextValue == null ? null : roundTo(nextValue / scale, storedDecimals);

    // Clearing a required field means 0, so the schema still sees a number. An optional one is
    // allowed to be empty.
    const cleanedValue = stored ?? (isRequired ? 0 : null);

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
          value={displayValue}
          onChange={(e: InputNumberChangeEvent) => handleChange(e.value)}
          onBlur={onBlur}
          placeholder={placeholderText}
          min={min}
          max={max}
          suffix={isPercent ? " %" : undefined}
          maxFractionDigits={decimals}
          disabled={isDisabled}
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
