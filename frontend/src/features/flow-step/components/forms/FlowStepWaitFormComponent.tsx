import { FlowStepWaitSchema } from "@/features/flow-step/schema/base-flow-step-wait.zod";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { classNames } from "primereact/utils";
import { Controller, useForm } from "react-hook-form";

interface Props {
  defaultValues?: Partial<FlowStepDto>;
  onSubmit: (data: FlowStepDto) => void;
}

export default function FlowStepWaitForm({ defaultValues, onSubmit }: Props) {
  const form = useForm<FlowStepDto>({
    resolver: zodResolver(FlowStepWaitSchema),
    mode: "onChange",
    defaultValues: new FlowStepDto(),
  });

  const {
    control,
    handleSubmit,
    formState: { isValid, errors },
  } = form;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="p-fluid"
    >
      <div className="field">
        <label>Name *</label>
        <Controller
          name="name"
          control={control}
          render={({ field, fieldState }) => (
            <InputText
              {...field}
              className={classNames({ "p-invalid": fieldState.invalid })}
            />
          )}
        />
        {errors.name && (
          <small className="p-error">{errors.name.message}</small>
        )}
      </div>

      <div className="field">
        <label>Wait duration (ms) *</label>
        <Controller
          name="waitForMilliseconds"
          control={control}
          render={({ field, fieldState }) => (
            <InputNumber
              {...field}
              min={50}
              max={300000}
              className={classNames({ "p-invalid": fieldState.invalid })}
            />
          )}
        />
        {errors.waitForMilliseconds && (
          <small className="p-error">
            {errors.waitForMilliseconds.message}
          </small>
        )}
      </div>

      {/* Add location fields, orderNumber etc. via shared component if desired */}

      <Button
        type="submit"
        label="Save Wait Step"
        icon="pi pi-check"
        disabled={!isValid}
        className="mt-3"
      />
    </form>
  );
}
