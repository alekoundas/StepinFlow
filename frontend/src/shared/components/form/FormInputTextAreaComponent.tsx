import LabelComponent from "@/shared/components/LabelComponent";

import { classNames } from "primereact/utils";
import { useController } from "react-hook-form";
import { InputTextarea } from "primereact/inputtextarea";
import type { FormFieldName } from "@/shared/models/form-field-name";

interface Props {
  fieldName: FormFieldName;
  label: string;
  placeholderText?: string;
  hintText?: string;
  rows?: number;
  maxLength?: number;
  isDisabled?: boolean;
  isRequired?: boolean;
  className?: string;
}

export function FormInputTextAreaComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
  rows = 3,
  maxLength,
  isDisabled = false,
  isRequired = false,
  className,
}: Props) {
  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController({ name: fieldName });

  const text = value ? value.toString() : "";

  return (
    <>
      <div className={classNames("field", className)}>
        <div className="flex justify-content-between align-items-end">
          <LabelComponent
            text={label}
            weight="bold"
            isRequired={isRequired}
          />
          <LabelComponent
            text={`${text.length} / ${maxLength}`}
            size="xs"
            color="secondary"
            hidden={maxLength === undefined}
          />
        </div>
        <InputTextarea
          ref={ref}
          name={fieldName}
          value={text}
          onChange={(e) => onChange(e.target.value)}
          onBlur={onBlur}
          placeholder={placeholderText}
          disabled={isDisabled}
          rows={rows}
          maxLength={maxLength}
          autoResize={true}
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
          color="error"
          className="mt-1"
        />
      </div>
    </>
  );
}
