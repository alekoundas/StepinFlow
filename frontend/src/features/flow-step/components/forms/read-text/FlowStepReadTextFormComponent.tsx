import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "primereact/button";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { ReadTextTestResultDto } from "@/shared/models/database/read-text-test-result-dto";
import { FlowStepReadTextSchema } from "@/features/flow-step/components/forms/read-text/flow-step-read-text.zod";
import { isWaitingMode } from "@/features/flow-step/components/forms/shared/search-modes";
import FlowStepReadTextFormFieldsComponent from "@/features/flow-step/components/forms/read-text/FlowStepReadTextFormFieldsComponent";
import FlowStepReadTextTestPanelComponent from "@/features/flow-step/components/forms/read-text/FlowStepReadTextTestPanelComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepReadTextFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepReadTextSchema>>({
    resolver: zodResolver(FlowStepReadTextSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<ReadTextTestResultDto | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const handleTest = async () => {
    setIsTesting(true);
    try {
      setTestResult(
        await backendApiService.FlowStep.testReadText(buildDto(form.getValues())),
      );
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  const buildDto = (data: z.infer<typeof FlowStepReadTextSchema>) =>
    new FlowStepDto({
      ...defaultValues,
      ...data,
      flowAreaId: data.flowAreaId ?? undefined,
    });

  const handleSubmit = (data: z.infer<typeof FlowStepReadTextSchema>) =>
    onSubmit(buildDto(data));

  return (
    <>
      <FormHeaderComponent
        title="Read Text Step Configuration"
        description="Read the text inside an area, branch on whether it matches, and hand it to later steps."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepReadTextFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            isDisabled={formMode === "VIEW"}
          />

          <div>
            <Button
              type="button"
              label={isTesting ? "Reading..." : "Test"}
              icon="pi pi-play"
              loading={isTesting}
              disabled={!isValid || isTesting}
              onClick={handleTest}
              className="p-button-outlined"
              tooltip="Reads the area now so you can see what Windows makes of it"
              tooltipOptions={{ position: "top" }}
            />
          </div>

          {testResult && (
            <FlowStepReadTextTestPanelComponent
              result={testResult}
              isWaiting={isWaitingMode(form.getValues("searchMode"))}
            />
          )}

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </>
  );
}
