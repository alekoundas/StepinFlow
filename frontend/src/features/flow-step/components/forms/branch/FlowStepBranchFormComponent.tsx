import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { useFlowStep } from "@/features/flow-step/hooks/use-flow-step";
import { FlowStepBranchSchema } from "@/features/flow-step/components/forms/branch/flow-step-branch.zod";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

// What each kind of check has done when it goes on to its Success steps, and to its Failure steps.
const OUTCOMES: Partial<Record<FlowStepTypeEnum, [string, string]>> = {
  [FlowStepTypeEnum.SEARCH_IMAGE]: ["finds what it looks for", "does not find what it looks for"],
  [FlowStepTypeEnum.SEARCH_TEXT]: ["reads what it looks for", "does not read what it looks for"],
  [FlowStepTypeEnum.CHECK_VALUE]: ["finds the condition holds", "finds the condition does not hold"],
  [FlowStepTypeEnum.SYSTEM_COMMAND]: ["ends on a success exit code", "ends on any other exit code"],
  [FlowStepTypeEnum.WINDOW_FOCUS]: ["finds the window", "does not find the window"],
  [FlowStepTypeEnum.WINDOW_RESIZE]: ["finds the window", "does not find the window"],
  [FlowStepTypeEnum.WINDOW_RELOCATE]: ["finds the window", "does not find the window"],
};

/** A Success or a Failure row: no settings of its own, only why it is there. */
export default function FlowStepBranchFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepBranchSchema>>({
    resolver: zodResolver(FlowStepBranchSchema),
    mode: "onChange",
    defaultValues: { codeComment: defaultValues.codeComment },
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  const { data: parent } = useFlowStep(defaultValues.parentFlowStepId ?? null);

  const isSuccess = defaultValues.flowStepType === FlowStepTypeEnum.SUCCESS;
  const parentName = parent ? `“${parent.name}”` : "the step it belongs to";
  const [succeeds, fails] = (parent && OUTCOMES[parent.flowStepType]) ?? ["succeeds", "fails"];

  const summary = `Executes when ${parentName} ${isSuccess ? succeeds : fails}.`;

  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit((data) =>
          onSubmit(new FlowStepDto({ ...defaultValues, codeComment: data.codeComment ?? "" })),
        )}
      >
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
          summary={summary}
        >
          <LabelComponent
            text="A branch has no settings of its own: its name is fixed, and what it does is the steps under it."
            size="sm"
            color="secondary"
          />
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
