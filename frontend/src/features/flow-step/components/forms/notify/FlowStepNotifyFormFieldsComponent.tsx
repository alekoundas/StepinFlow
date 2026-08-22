import { useState } from "react";
import { useFormContext } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { InputTextarea } from "primereact/inputtextarea";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import DiscordBotFormComponent from "@/features/discord-bot/components/form/DiscordBotFormComponent";
import { useDiscordBotMutations } from "@/features/discord-bot/hooks/use-discord-bot";

const BOT_FORM_ID = "discord-bot-form";

interface IdOption {
  label: string;
  value: number;
  description?: string;
}

interface Props {
  /** In ADD mode there is no step row yet, so the branch it will live under stands in for it. */
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepNotifyFormFieldsComponent({
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { setValue, watch } = useFormContext();
  const { openForm, closeAll } = useDialogStore();
  const { createDiscordBotMutation } = useDiscordBotMutations();

  const referenceId = watch("flowStepReferenceId") as number | null | undefined;
  const [isReportingFailure, setIsReportingFailure] = useState(referenceId != null);

  const loadBots = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.discordBot({ searchText: filter }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
        description: item.description,
      })),
    );

  // The steps that fail above this one. Empty is a correct and common answer - a Notify step
  // outside any Failure branch has nothing to report on.
  const { data: failedSteps = [] } = useQuery({
    queryKey: ["lookup", "failedStep", parentFlowStepId],
    queryFn: () =>
      backendApiService.Lookup.failedStep({ flowStepId: parentFlowStepId }).then((res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
          description: item.description,
        })),
      ),
    enabled: parentFlowStepId != null,
  });

  // Saved straight away so the dropdown has a real id to bind to, same as areas and locations.
  const openAddBot = () =>
    openForm(BOT_FORM_ID, {
      headerText: "Add Discord bot",
      formId: BOT_FORM_ID,
      children: (
        <DiscordBotFormComponent
          formId={BOT_FORM_ID}
          formMode="ADD"
          defaultValues={new DiscordBotDto()}
          isFormInDialog={true}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const newId = await createDiscordBotMutation.mutateAsync({ ...data, id: 0 });
            setValue("discordBotId", newId, { shouldValidate: true, shouldDirty: true });
          }}
        />
      ),
    });

  const toggleReporting = (isOn: boolean) => {
    setIsReportingFailure(isOn);

    if (!isOn)
      setValue("flowStepReferenceId", null, { shouldValidate: true, shouldDirty: true });
  };

  return (
    <div className="flex flex-column gap-2 mt-4">
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        hintText="What this step is called in the tree."
        isRequired={true}
        isDisabled={isDisabled}
      />

      <div className="flex gap-3 align-items-end">
        <div className="flex-1">
          <FormDropdownComponent<FlowStepDto, IdOption>
            fieldName="discordBotId"
            labelText="Send through"
            mode="remote"
            queryKey={["lookup", "discordBot"]}
            queryFn={loadBots}
            optionLabel="label"
            optionValue="value"
            placeholderText="Select a bot..."
            isRequired={true}
            isDisabled={isDisabled}
            hintText="Messages are throttled per bot. Anything sent inside the gap is dropped rather than queued, so a Notify step in a retry loop sends once."
            itemTemplate={(item) => (
              <div className="flex flex-column">
                <LabelComponent text={item.label} />
                {item.description && (
                  <LabelComponent
                    text={item.description}
                    size="xs"
                    color="secondary"
                  />
                )}
              </div>
            )}
          />
        </div>

        <Button
          type="button"
          icon="pi pi-plus"
          label="New"
          onClick={openAddBot}
          disabled={isDisabled}
          className="p-button-outlined mb-3"
          tooltip="Add a Discord bot and use it here"
          tooltipOptions={{ position: "top" }}
        />
      </div>

      <div className="flex flex-column gap-1 mt-2">
        <LabelComponent
          text="Message"
          size="sm"
        />
        <InputTextarea
          rows={3}
          autoResize
          disabled={isDisabled}
          placeholder="Broke overnight, check the VPN."
          value={(watch("notifyMessage") as string) ?? ""}
          onChange={(e) =>
            setValue("notifyMessage", e.target.value, { shouldDirty: true })
          }
        />
        <LabelComponent
          text="Optional. The flow name is always sent, whether you write anything here or not."
          size="xs"
          color="secondary"
        />
      </div>

      <div className="mt-3 flex flex-column gap-2">
        <div className="flex align-items-center gap-2">
          <input
            id="notify-report-failure"
            type="checkbox"
            checked={isReportingFailure}
            disabled={isDisabled || failedSteps.length === 0}
            onChange={(e) => toggleReporting(e.target.checked)}
          />
          <label htmlFor="notify-report-failure">
            <LabelComponent text="Say why a step failed" />
          </label>
        </div>

        {failedSteps.length === 0 ? (
          <Message
            severity="info"
            className="justify-content-start"
            text="Nothing fails above this step, so there is no failure to describe. Move it into a Failure branch to report one."
          />
        ) : (
          isReportingFailure && (
            <FormDropdownComponent<FlowStepDto, IdOption>
              fieldName="flowStepReferenceId"
              labelText="Which step"
              mode="local"
              options={failedSteps}
              optionLabel="label"
              optionValue="value"
              placeholderText="Select the step..."
              isDisabled={isDisabled}
              hintText="Only steps that fail above this one, nearest first. The message says what it was trying to do."
            />
          )
        )}
      </div>
    </div>
  );
}
