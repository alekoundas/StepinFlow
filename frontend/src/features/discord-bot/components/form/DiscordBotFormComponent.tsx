import type z from "zod";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { Divider } from "primereact/divider";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import {
  RATE_LIMIT_MAX_SECONDS,
  RATE_LIMIT_MIN_SECONDS,
  type DiscordBotDto,
} from "@/shared/models/database/discord-bot-dto";
import { DiscordBotSchema } from "@/features/discord-bot/components/form/discord-bot.zod";
import DiscordBotTestButtonComponent from "@/features/discord-bot/components/form/DiscordBotTestButtonComponent";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: DiscordBotDto;
  isFormInDialog?: boolean;

  onSubmit: (formValues: DiscordBotDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function DiscordBotFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof DiscordBotSchema>>({
    resolver: zodResolver(DiscordBotSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  const isDisabled = formMode === "VIEW";

  return (
    <div>
      <FormHeaderComponent
        formMode={formMode}
        title="Discord Bot"
        description="A channel to post to, and the name and picture the messages appear under. Notify steps pick one of these."
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          id={formId}
          onSubmit={form.handleSubmit((partialDto) =>
            onSubmit({ ...defaultValues, ...partialDto } as DiscordBotDto),
          )}
          className="flex flex-column h-full"
        >
          <div className="flex flex-column gap-2 mt-4">
            <FormInputTextComponent
              fieldName="name"
              label="Name"
              placeholderText="Alerts"
              hintText="What you pick from in a Notify step."
              isRequired={true}
              isDisabled={isDisabled}
            />

            <FormInputTextComponent
              fieldName="webhookUrl"
              label="Webhook URL"
              placeholderText="https://discord.com/api/webhooks/..."
              hintText="Discord: channel settings, Integrations, Webhooks, New Webhook, Copy Webhook URL. Anyone holding this link can post to that channel."
              isRequired={true}
              isDisabled={isDisabled}
            />

            {!isDisabled && <DiscordBotTestButtonComponent />}

            <Divider />

            <LabelComponent
              text="How it appears in Discord"
              weight="semibold"
              size="sm"
            />

            <FormInputTextComponent
              fieldName="botName"
              label="Display name"
              placeholderText="StepinFlow"
              hintText="Leave empty to keep whatever the webhook was set up with in Discord."
              isDisabled={isDisabled}
            />

            <FormInputTextComponent
              fieldName="avatarUrl"
              label="Avatar URL"
              placeholderText="https://..."
              hintText="Discord fetches this itself, so it has to be a link to an image rather than a file on this machine."
              isDisabled={isDisabled}
            />

            <Divider />

            <FormInputNumberComponent
              fieldName="rateLimitSeconds"
              label="Shortest gap between messages (seconds)"
              min={RATE_LIMIT_MIN_SECONDS}
              max={RATE_LIMIT_MAX_SECONDS}
              hintText={`Applies to this bot only. Anything arriving inside the gap is dropped rather than queued, so a Notify step in a retry loop sends once instead of a hundred times. ${RATE_LIMIT_MIN_SECONDS}-${RATE_LIMIT_MAX_SECONDS}.`}
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>

          {!isFormInDialog && (
            <FormFooterComponent
              formMode={formMode}
              isValid={isValid}
              isDirty={isDirty}
              onCancel={onCancel}
            />
          )}
        </form>
      </FormProvider>
    </div>
  );
}
