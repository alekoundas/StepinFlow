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
import type { TextSearchTestResultDto } from "@/shared/models/database/text-search-test-result-dto";
import { FlowStepTextSearchSchema } from "@/features/flow-step/components/forms/text-search/flow-step-text-search.zod";
import FlowStepTextSearchFormFieldsComponent from "@/features/flow-step/components/forms/text-search/FlowStepTextSearchFormFieldsComponent";
import FlowStepTextSearchTestPanelComponent from "@/features/flow-step/components/forms/text-search/FlowStepTextSearchTestPanelComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepTextSearchFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepTextSearchSchema>>({
    resolver: zodResolver(FlowStepTextSearchSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<TextSearchTestResultDto | null>(null);

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
        await backendApiService.FlowStep.testTextSearch(buildDto(form.getValues())),
      );
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  const buildDto = (data: z.infer<typeof FlowStepTextSearchSchema>) =>
    new FlowStepDto({
      ...defaultValues,
      ...data,
      flowSearchAreaId: data.flowSearchAreaId ?? undefined,
    });

  const handleSubmit = (data: z.infer<typeof FlowStepTextSearchSchema>) =>
    onSubmit(buildDto(data));

  return (
    <>
      <FormHeaderComponent
        title="Text Search Step Configuration"
        description="Read the text inside a search area, branch on whether it matches, and hand it to later steps."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepTextSearchFormFieldsComponent
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

          {testResult && <FlowStepTextSearchTestPanelComponent result={testResult} />}

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
