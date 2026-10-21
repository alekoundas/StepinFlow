import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "primereact/button";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { SearchTextTestResultDto } from "@/shared/models/database/search-text-test-result-dto";
import { FlowStepSearchTextSchema } from "@/features/flow-step/components/forms/search-text/flow-step-search-text.zod";
import FlowStepSearchTextFormFieldsComponent from "@/features/flow-step/components/forms/search-text/FlowStepSearchTextFormFieldsComponent";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import FlowStepSearchTextTestPanelComponent from "@/features/flow-step/components/forms/search-text/FlowStepSearchTextTestPanelComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepSearchTextFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepSearchTextSchema>>({
    resolver: zodResolver(FlowStepSearchTextSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<SearchTextTestResultDto | null>(null);

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
        await backendApiService.FlowStep.testSearchText(buildDto(form.getValues())),
      );
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  const buildDto = (data: z.infer<typeof FlowStepSearchTextSchema>) =>
    new FlowStepDto({
      ...defaultValues,
      ...data,
      flowAreaId: data.flowAreaId ?? undefined,
    });

  const handleSubmit = (data: z.infer<typeof FlowStepSearchTextSchema>) =>
    onSubmit(buildDto(data));

  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit(handleSubmit)}>
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
          testAction={
            <Button
              type="button"
              label={isTesting ? "Reading..." : "Test now"}
              icon="pi pi-play"
              loading={isTesting}
              disabled={!isValid || isTesting}
              onClick={handleTest}
              outlined
              tooltip="Reads the area now so you can see what Windows makes of it"
              tooltipOptions={{ position: "top" }}
            />
          }
        >
          <FlowStepSearchTextFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            isDisabled={formMode === "VIEW"}
          />

          {testResult && (
            <FlowStepSearchTextTestPanelComponent result={testResult} />
          )}
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
