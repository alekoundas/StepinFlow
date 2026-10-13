import { useState } from "react";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";

import { FlowFormFieldsComponent } from "@/features/flow/components/form/FlowFormFieldsComponent";
import FlowAreaScalesWithFieldComponent from "@/features/flow-area/components/forms/FlowAreaScalesWithFieldComponent";
import {
  RecordingSetupSchema,
  type RecordingSetupForm,
} from "@/features/wizard/components/recording-setup.zod";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import WindowMatchFieldsComponent from "@/shared/components/form/WindowMatchFieldsComponent";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { backendApiService } from "@/shared/services/backend-api-service";

interface Props {
  isSubFlow: boolean;
  onCreated: (flowId: number, addsLaunchStep: boolean) => void;
}

const DEFAULT_VALUES: RecordingSetupForm = {
  name: "",
  description: "",
  isQaTest: false,
  processName: "",
  titlePattern: "",
  titleMatchMode: TitleMatchModeEnum.CONTAINS,
  useClientArea: true,
  scalesWith: null,
  addsLaunchStep: true,
};

/**
 * What the recorder needs before the first click: the flow's name and description, and the window
 * it works in, which becomes its main area. The flow is saved here, so the name is reserved and the
 * recording is made against the main area.
 */
export default function RecordingSetupComponent({ isSubFlow, onCreated }: Props) {
  const form = useForm<RecordingSetupForm>({
    resolver: zodResolver(RecordingSetupSchema),
    mode: "onChange",
    defaultValues: DEFAULT_VALUES,
  });

  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (values: RecordingSetupForm) => {
    setIsCreating(true);
    setError(null);

    try {
      const flowId = await backendApiService.Recording.setup({
        ...values,
        name: values.name.trim(),
        isSubFlow,
        scalesWith: values.scalesWith ?? null,
      });

      onCreated(flowId, values.addsLaunchStep);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit(submit)}
        className="flex flex-column gap-3"
      >
        <Panel header="The flow">
          <FlowFormFieldsComponent />

          <FormInputCheckboxComponent
            fieldName="isQaTest"
            label="This is a QA test"
            hintText="A test checks the application works; an automation only does a task."
          />
        </Panel>

        <Panel header="The window it works in">
          <WindowMatchFieldsComponent
            useClientArea={true}
            bringsToFront={true}
          />

          <div className="mt-3">
            <FormInputCheckboxComponent
              fieldName="addsLaunchStep"
              label="Also add a step that launches the app"
              hintText="The flow's first step starts it, so it does not have to be open already."
            />
          </div>

          <FlowAreaScalesWithFieldComponent
            type={FlowAreaTypeEnum.APPLICATION}
            parentName={null}
          />
        </Panel>

        {error && (
          <Message
            severity="error"
            className="w-full justify-content-start"
            text={error}
          />
        )}

        <div className="flex justify-content-end">
          <Button
            type="submit"
            label="Create and continue"
            icon="pi pi-check"
            loading={isCreating}
            disabled={!form.formState.isValid || isCreating}
          />
        </div>
      </form>
    </FormProvider>
  );
}
