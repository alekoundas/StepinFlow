import { useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { Controller, FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { InputSwitch } from "primereact/inputswitch";
import { Message } from "primereact/message";

import FlowAreaScalesWithFieldComponent from "@/features/flow-area/components/forms/FlowAreaScalesWithFieldComponent";
import { DESCRIPTION_MAX_LENGTH } from "@/features/flow/components/form/flow.zod";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import RecordingWindowPickerComponent from "@/features/wizard/components/RecordingWindowPickerComponent";
import {
  RecordingSetupSchema,
  type RecordingSetupForm,
} from "@/features/wizard/components/recording-setup.zod";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import { FormInputTextAreaComponent } from "@/shared/components/form/FormInputTextAreaComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { backendApiService } from "@/shared/services/backend-api-service";
import { toHotkeyDisplay } from "@/shared/utils/hotkey";

// The theme's own variables: PrimeFlex 4's surface and text-color classes read tokens this theme
// does not define.
const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  isSubFlow: boolean;
  onCreated: (flowId: number, addsLaunchStep: boolean) => void;
  onCancel: () => void;
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
 * What the recorder needs before the first click: the flow's name and what it is for, and the
 * window it works in, which becomes its main area. Record now saves the flow, so the name is
 * reserved and the recording is made against the main area, and opens the recording bar.
 */
export default function RecordingSetupComponent({ isSubFlow, onCreated, onCancel }: Props) {
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
        className="flex flex-column gap-4"
      >
        <div className="flex flex-wrap align-items-start gap-4">
          <div
            className="flex flex-column gap-3"
            style={{ flex: "999 1 520px", minWidth: 0 }}
          >
            <SectionComponent title="The flow">
              <FormInputTextComponent
                fieldName="name"
                label="Name"
                isRequired={true}
              />
              <FormInputTextAreaComponent
                fieldName="description"
                label="What should it do?"
                placeholderText="Sign in to the billing portal, open Invoices and download last month's."
                hintText="In your own words. It becomes the flow's description, and the list shows its first line."
                maxLength={DESCRIPTION_MAX_LENGTH}
              />
            </SectionComponent>

            <SectionComponent title="What it works in">
              <RecordingWindowPickerComponent>
                <FormInputCheckboxComponent
                  fieldName="addsLaunchStep"
                  label="Also add a step that launches the app"
                  hintText="The flow's first step starts it, so it does not have to be open already."
                />
                <FlowAreaScalesWithFieldComponent
                  type={FlowAreaTypeEnum.APPLICATION}
                  parentName={null}
                />
              </RecordingWindowPickerComponent>
            </SectionComponent>

            <SectionComponent title="Kind">
              <Controller
                name="isQaTest"
                control={form.control}
                render={({ field }) => (
                  <div className="flex align-items-start gap-3">
                    <InputSwitch
                      inputId="recording-qa-test"
                      checked={field.value}
                      onChange={(e) => field.onChange(e.value)}
                    />
                    <label
                      htmlFor="recording-qa-test"
                      className="flex flex-column gap-1 cursor-pointer"
                    >
                      <span className="font-medium">This flow is a QA test</span>
                      <span style={MUTED}>
                        Off, it is an automation: it only does a task. On, it is a test: it checks the
                        application works.
                      </span>
                    </label>
                  </div>
                )}
              />
            </SectionComponent>
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
              The flow is saved as you press it, so its name is yours.
            </span>
            <Button
              type="submit"
              label="Record now"
              icon="pi pi-circle-fill"
              severity="danger"
              loading={isCreating}
              disabled={!form.formState.isValid || isCreating}
            />
          </div>
        </div>
      </form>
    </FormProvider>
  );
}

function SectionComponent({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section
      className="flex flex-column gap-3 p-4 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        className="m-0 text-sm font-semibold uppercase"
        style={{ ...MUTED, letterSpacing: "0.06em" }}
      >
        {title}
      </h2>
      {children}
    </section>
  );
}

// The keys are the ones in Settings, so what this says is what works.
function WhileYouRecordComponent() {
  const { data: settings = [] } = useAppSettings();

  const pauseKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_PAUSE)?.value;
  const stopKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_STOP)?.value;

  return (
    <section
      className="flex flex-column gap-3 p-4 border-1 border-round-xl"
      style={CARD}
    >
      <h2 className="m-0 text-base font-semibold">While you record</h2>
      <p
        className="m-0"
        style={MUTED}
      >
        StepinFlow steps aside to a small bar that stays on top. Arrange your windows, then press
        Start on it. Clicks on the bar are never recorded, and it never shows up in a screenshot.
      </p>
      <dl
        className="m-0 align-items-center"
        style={{ display: "grid", gridTemplateColumns: "auto 1fr", gap: "10px 14px" }}
      >
        <dt>
          <KeysComponent keys={pauseKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          Pause or resume
        </dd>
        <dt>
          <KeysComponent keys={stopKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          Stop and review
        </dd>
      </dl>
      <span
        className="text-xs"
        style={MUTED}
      >
        Both work whatever window is in front, and are never recorded. They are under Settings, Recording
        keys.
      </span>
    </section>
  );
}

function KeysComponent({ keys }: { keys: string | undefined }) {
  if (!keys)
    return (
      <span
        className="text-sm"
        style={MUTED}
      >
        Not set
      </span>
    );

  return (
    <kbd
      className="text-xs border-1 border-round-md px-2 py-1"
      style={{ fontFamily: "inherit", borderColor: "var(--surface-border)", borderBottomWidth: 2 }}
    >
      {toHotkeyDisplay(keys)}
    </kbd>
  );
}
