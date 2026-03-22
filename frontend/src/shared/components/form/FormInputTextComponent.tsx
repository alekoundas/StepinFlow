import LabelComponent from "@/shared/components/LabelComponent";
import { InputText } from "primereact/inputtext";
import { classNames } from "primereact/utils";
import { useController, useFormContext } from "react-hook-form";

interface Props {
  name: string;
  label: string;
  isDisabled?: boolean;
}

export function FormInputTextComponent({
  name,
  label,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const {
    field: { ref, ...field },
    fieldState: { invalid, error },
  } = useController({ name, control });
  return (
    <>
      <div className="field">
        <LabelComponent
          text={label}
          weight="bold"
        />
        <InputText
          ref={ref}
          disabled={isDisabled}
          className={classNames({ "p-invalid": invalid })}
        />
        <LabelComponent
          text={label}
          weight="bold"
          className={classNames({ "p-error": error })}
        />
        <small className="p-error mt-1 block">{error?.message}</small>
      </div>
    </>
  );
}
