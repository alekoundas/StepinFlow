import { useFormContext, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputTextAreaComponent } from "@/shared/components/form/FormInputTextAreaComponent";
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

const withDescription = (item: IdOption) => (
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
);

export default function FlowStepNotifyFormFieldsComponent({
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();
  const { openForm, closeAll } = useDialogStore();
  const { createDiscordBotMutation } = useDiscordBotMutations();

  const referenceId = useWatch({ control, name: "flowStepReferenceId" }) as number | null | undefined;

  const loadBots = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.discordBot({ searchText: filter }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
        description: item.description,
      })),
    );

  // The steps it sits under, either side. Empty is a correct answer - a Notify step under no
  // check has no result to include.
  const { data: reportedSteps = [] } = useQuery({
    queryKey: ["lookup", "reportedStep", parentFlowStepId],
    queryFn: () =>
      backendApiService.Lookup.reportedStep({ flowStepId: parentFlowStepId }).then((res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
          description: item.description,
        })),
      ),
    enabled: parentFlowStepId != null,
    staleTime: 0,
    gcTime: 0,
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
            itemTemplate={withDescription}
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

      {/* Still shown when it names a step it no longer sits under, so that one can be cleared. */}
      {reportedSteps.length > 0 || referenceId != null ? (
        <FormDropdownComponent<FlowStepDto, IdOption>
          fieldName="flowStepReferenceId"
          labelText="Include the result of"
          mode="local"
          options={reportedSteps}
          optionLabel="label"
          optionValue="value"
          placeholderText="Nothing, just the message"
          isDisabled={isDisabled}
          hintText="Goes above your message: what an image search found and how close it came, the text a search read, what a command printed. Only steps this one sits under, nearest first."
          itemTemplate={withDescription}
        />
      ) : (
        <Message
          severity="info"
          className="justify-content-start mb-3"
          text="This step sits under no step with a result to include. Put it in a step's Success or Failure branch to include one."
        />
      )}

      <FormInputTextAreaComponent
        fieldName="message"
        label="Message"
        placeholderText="Broke overnight, check the VPN."
        hintText="Optional. The flow name is always sent, whether you write anything here or not."
        rows={3}
        maxLength={1500}
        isDisabled={isDisabled}
      />
    </div>
  );
}
