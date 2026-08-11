import LabelComponent from "@/shared/components/LabelComponent";

import { classNames } from "primereact/utils";
import { SelectButton } from "primereact/selectbutton";
import { useController, type FieldValues, type Path } from "react-hook-form";

interface Option<TValue> {
  label: string;
  value: TValue;
  icon?: string;
  disabled?: boolean;
}

interface Props<TForm extends FieldValues, TValue> {
  fieldName: Path<TForm>;
  labelText?: string;
  hintText?: string;
  options: Option<TValue>[];
  isDisabled?: boolean;
  isRequired?: boolean;
  className?: string;
  classNameContainer?: string;
}

export function FormSelectButtonComponent<TForm extends FieldValues, TValue>({
  fieldName,
  labelText,
  hintText,
  options,
  isDisabled = false,
  isRequired = false,
  className,
  classNameContainer,
}: Props<TForm, TValue>) {
  const {
    field: { value, onChange, onBlur },
    fieldState: { invalid, error },
  } = useController<TForm>({ name: fieldName });

  return (
    <div className={classNames("field", classNameContainer)}>
      <LabelComponent
        text={labelText ?? ""}
        weight="bold"
        isRequired={isRequired}
        hidden={labelText === undefined}
      />

      <SelectButton
        value={value}
        options={options}
        optionLabel="label"
        optionValue="value"
        optionDisabled="disabled"
        disabled={isDisabled}
        onBlur={onBlur}
        // SelectButton clears to null when the active item is clicked again; a required
        // single-choice field should stay on what it had.
        onChange={(e) => (e.value !== null ? onChange(e.value) : null)}
        className={classNames(className, { "p-invalid": invalid })}
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
  );
}
