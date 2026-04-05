import LabelComponent from "@/shared/components/LabelComponent";

import { classNames } from "primereact/utils";
import { useController } from "react-hook-form";
import { InputText } from "primereact/inputtext";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";

interface Props {
  fieldName: keyof FlowStepDto | keyof FlowDto;
  label: string;
  placeholderText?: string;
  hintText?: string;
  isDisabled?: boolean;
  isRequired?: boolean;
}

export function FormInputTextComponent({
  fieldName,
  label,
  placeholderText,
  hintText,
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
          isRequired={isRequired}
        />
        <InputText
          ref={ref}
          name={fieldName}
          value={value ? value.toString() : ""}
          onChange={(e) => onChange(e.target.value)}
          onBlur={onBlur}
          placeholder={placeholderText}
          disabled={isDisabled}
          // required={isRequired}
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
