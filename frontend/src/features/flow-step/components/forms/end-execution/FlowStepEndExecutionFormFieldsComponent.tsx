import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { FlowStepEndExecutionSchema } from "@/features/flow-step/components/forms/end-execution/flow-step-end-execution.zod";

type EndExecutionForm = z.infer<typeof FlowStepEndExecutionSchema>;

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepEndExecutionFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const asSuccess = useWatch({ control, name: "endExecutionAsSuccess" }) as boolean;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <FormSelectButtonComponent<EndExecutionForm, boolean>
        fieldName="endExecutionAsSuccess"
        labelText="Verdict"
        options={[
          { label: "Failed", value: false, icon: "pi pi-times" },
          { label: "Passed", value: true, icon: "pi pi-check" },
        ]}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={
          asSuccess
            ? "Stops here and reports a pass. Everything below this step is skipped."
            : "Stops here and reports a failure. Put any cleanup - logging out, closing a dialog - above this step."
        }
      />

      <FormInputTextComponent
        fieldName="message"
        label={asSuccess ? "Message (optional)" : "Reason"}
        placeholderText={asSuccess ? "" : "did not reach the products page"}
        isRequired={!asSuccess}
        isDisabled={isDisabled}
        hintText="Shown against this step in the report and in the failure notification."
      />
    </>
  );
}
