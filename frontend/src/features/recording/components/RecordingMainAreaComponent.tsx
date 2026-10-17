import { useState } from "react";
import type { CSSProperties } from "react";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import FlowAreaScalesWithFieldComponent from "@/features/flow-area/components/forms/FlowAreaScalesWithFieldComponent";
import RecordingSectionComponent from "@/features/recording/components/RecordingSectionComponent";
import RecordingWindowPickerComponent from "@/features/recording/components/RecordingWindowPickerComponent";
import WhileYouRecordComponent from "@/features/recording/components/WhileYouRecordComponent";
import {
  RecordingWindowSchema,
  type RecordingWindowForm,
} from "@/features/recording/components/recording-setup.zod";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import { backendApiService } from "@/shared/services/backend-api-service";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const MAIN_AREA_NAME = "Main window";

interface Props {
  flowId: number;
  onSaved: () => void;
  onCancel: () => void;
}

const DEFAULT_VALUES: RecordingWindowForm = {
  processName: "",
  titlePattern: "",
  titleMatchMode: TitleMatchModeEnum.CONTAINS,
  useClientArea: true,
  scalesWith: null,
};

/**
 * The window a flow that already existed works in, for one with no main area yet: a recording is
 * made against it, and the steps it writes search in it. Record now saves it as the flow's main
 * area and opens the recording bar.
 */
export default function RecordingMainAreaComponent({ flowId, onSaved, onCancel }: Props) {
  const form = useForm<RecordingWindowForm>({
    resolver: zodResolver(RecordingWindowSchema),
    mode: "onChange",
    defaultValues: DEFAULT_VALUES,
  });

  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (values: RecordingWindowForm) => {
    setIsSaving(true);
    setError(null);

    // Named after the app, as the recording setup names it.
    const processName = values.processName.trim();

    try {
      await backendApiService.FlowArea.create(
        new FlowAreaDto({
          flowId,
          name: processName || MAIN_AREA_NAME,
          type: FlowAreaTypeEnum.APPLICATION,
          isMain: true,
          processName,
          titlePattern: values.titlePattern.trim(),
          titleMatchMode: values.titleMatchMode,
          useClientArea: values.useClientArea,
          scalesWith: values.scalesWith ?? null,
        }),
      );

      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit(submit)}
        className="flex flex-column gap-4"
      >
        <div className="flex flex-wrap align-items-start gap-4">
          <div
            className="flex flex-column gap-3"
            style={{ flex: "999 1 520px", minWidth: 0 }}
          >
            <RecordingSectionComponent title="What it works in">
              <span style={MUTED}>
                This flow has no main area yet. The window you pick becomes it: the recording is made
                against it, and every step it writes searches in it.
              </span>
              <RecordingWindowPickerComponent>
                <FlowAreaScalesWithFieldComponent
                  type={FlowAreaTypeEnum.APPLICATION}
                  parentName={null}
                />
              </RecordingWindowPickerComponent>
            </RecordingSectionComponent>
          </div>

          <aside
            className="flex flex-column gap-3"
            style={{ flex: "1 1 300px", minWidth: 0 }}
          >
            <WhileYouRecordComponent />
          </aside>
        </div>

        {error && (
          <Message
            severity="error"
            className="w-full justify-content-start"
            text={error}
          />
        )}

        <div
          className="flex align-items-center justify-content-between flex-wrap gap-3 pt-3 border-top-1"
          style={{ borderColor: "var(--surface-border)" }}
        >
          <Button
            type="button"
            label="Cancel"
            onClick={onCancel}
            severity="secondary"
            text
          />

          <div className="flex align-items-center flex-wrap gap-3">
            <span
              className="text-sm"
              style={MUTED}
            >
              It is saved as the flow's main area as you press it.
            </span>
            <Button
              type="submit"
              label="Record now"
              icon="pi pi-circle-fill"
              severity="danger"
              loading={isSaving}
              disabled={!form.formState.isValid || isSaving}
            />
          </div>
        </div>
      </form>
    </FormProvider>
  );
}
