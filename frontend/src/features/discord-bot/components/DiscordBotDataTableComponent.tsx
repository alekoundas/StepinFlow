import { useState } from "react";
import { Avatar } from "primereact/avatar";
import { Button } from "primereact/button";

import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { DataTableComponent } from "@/shared/components/data-table/DataTableComponent";
import { UsageCountTagComponent } from "@/shared/components/UsageCountTagComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";
import DiscordBotFormComponent from "@/features/discord-bot/components/form/DiscordBotFormComponent";
import { useDiscordBotMutations } from "@/features/discord-bot/hooks/use-discord-bot";

const FORM_ID = "discord-bot-form";
const REVEALED_CHARACTERS = 6;

export function DiscordBotDataTableComponent({
  className,
}: {
  className?: string;
}) {
  const { openForm, openConfirm, closeAll } = useDialogStore();
  const {
    createDiscordBotMutation,
    updateDiscordBotMutation,
    deleteDiscordBotMutation,
  } = useDiscordBotMutations();

  const openEditor = (formMode: "ADD" | "EDIT" | "VIEW", bot: DiscordBotDto) =>
    openForm(FORM_ID, {
      headerText:
        formMode === "ADD"
          ? "Add Discord bot"
          : formMode === "EDIT"
            ? "Edit Discord bot"
            : bot.name,
      formId: FORM_ID,
      children: (
        <DiscordBotFormComponent
          formId={FORM_ID}
          formMode={formMode}
          defaultValues={bot}
          isFormInDialog={true}
          onEdit={() => openEditor("EDIT", bot)}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();

            if (formMode === "EDIT") {
              await updateDiscordBotMutation.mutateAsync({
                ...data,
                id: bot.id,
              });
              return;
            }

            await createDiscordBotMutation.mutateAsync({ ...data, id: 0 });
          }}
        />
      ),
    });

  const handleDelete = (bot: DiscordBotDto) => {
    openConfirm("discord-bot-delete", {
      headerText: `Delete ${bot.name}?`,
      confirmLabel: "Delete the bot",
      confirmSeverity: "danger",
      children: (
        <LabelComponent
          text={
            bot.flowStepsCount > 0
              ? `${bot.flowStepsCount} step(s) send through this bot. Deleting it will be refused until they point somewhere else.`
              : "Nothing sends through this bot. This cannot be undone."
          }
        />
      ),
      onConfirm: async () => {
        try {
          await deleteDiscordBotMutation.mutateAsync(bot.id);
        } catch (err) {
          openConfirm("discord-bot-delete-refused", {
            headerText: "Still in use",
            hideConfirm: true,
            cancelLabel: "Close",
            children: (
              <LabelComponent
                text={err instanceof Error ? err.message : String(err)}
              />
            ),
          });
        }
      },
    });
  };

  const columns: DataTableColumnDto<DiscordBotDto>[] = [
    {
      field: "name",
      header: "Name",
      sortable: true,
      body: (row: DiscordBotDto) => <LabelComponent text={row.name} />,
    },
    {
      field: "botName",
      header: "Posts as",
      body: (row: DiscordBotDto) => <PostsAsCell bot={row} />,
    },
    {
      field: "webhookUrl",
      header: "Webhook",
      body: (row: DiscordBotDto) => <MaskedUrlCell url={row.webhookUrl} />,
    },
    {
      field: "rateLimitSeconds",
      header: "Gap",
      body: (row: DiscordBotDto) => `${row.rateLimitSeconds}s`,
    },
    {
      field: "flowStepsCount",
      header: "Used By",
      body: (row: DiscordBotDto) => (
        <UsageCountTagComponent count={row.flowStepsCount} />
      ),
    },
    {
      field: "actions",
      header: "",
      body: (row: DiscordBotDto) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEditor("EDIT", new DiscordBotDto(row))}
          onClone={() =>
            openEditor(
              "ADD",
              new DiscordBotDto({
                ...row,
                id: 0,
                name: `${row.name} copy`,
                flowStepsCount: 0,
              }),
            )
          }
          onDelete={() => handleDelete(row)}
          extraActions={[
            {
              label: "View",
              icon: "pi pi-eye",
              command: () => openEditor("VIEW", new DiscordBotDto(row)),
            },
          ]}
        />
      ),
    },
  ];

  return (
    <div className={className}>
      <div className="flex justify-content-end mb-3">
        <Button
          type="button"
          label="Add Discord bot"
          icon="pi pi-plus"
          onClick={() => openEditor("ADD", new DiscordBotDto())}
        />
      </div>

      <DataTableComponent
        columns={columns}
        queryKey={["discordBots", "list"]}
        queryFn={(dto) => backendApiService.DiscordBot.getLazy(dto)}
      />
    </div>
  );
}

/** The avatar is the first remote image the app loads, so a dead link has to degrade quietly. */
function PostsAsCell({ bot }: { bot: DiscordBotDto }) {
  const [hasImage, setHasImage] = useState(true);

  const label = bot.botName || "(Discord default)";

  return (
    <div className="flex align-items-center gap-2">
      {bot.avatarUrl && hasImage ? (
        <Avatar
          image={bot.avatarUrl}
          shape="circle"
          size="normal"
          imageFallback="defaultimage"
          onImageError={() => setHasImage(false)}
        />
      ) : (
        <Avatar
          icon="pi pi-discord"
          shape="circle"
          size="normal"
        />
      )}

      <LabelComponent
        text={label}
        color={bot.botName ? "primary" : "secondary"}
        size="sm"
      />
    </div>
  );
}

function MaskedUrlCell({ url }: { url: string }) {
  const [isCopied, setIsCopied] = useState(false);

  const masked =
    url.length > REVEALED_CHARACTERS
      ? `••••••••${url.slice(-REVEALED_CHARACTERS)}`
      : "••••••••";

  const copy = async () => {
    await navigator.clipboard.writeText(url);
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 1500);
  };

  return (
    <div className="flex align-items-center gap-2">
      <span
        className="font-mono text-color-secondary text-sm"
        style={{ fontVariantNumeric: "tabular-nums" }}
      >
        {masked}
      </span>

      <Button
        type="button"
        icon={isCopied ? "pi pi-check" : "pi pi-copy"}
        text
        className="p-button-sm"
        aria-label="Copy webhook URL"
        tooltip={isCopied ? "Copied" : "Copy the full URL"}
        tooltipOptions={{ position: "top" }}
        onClick={() => void copy()}
      />
    </div>
  );
}
