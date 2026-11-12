import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Dropdown } from "primereact/dropdown";
import { ProgressBar } from "primereact/progressbar";
import { Skeleton } from "primereact/skeleton";

import StatusPillComponent, { type StatusPillSeverity } from "@/shared/components/StatusPillComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { AiLocalModelStateEnum } from "@/shared/enums/backend/ai/ai-local-model-state-enum";
import type { AiLocalModelStatusDto } from "@/shared/models/ai-local-model-status-dto";
import type { AiModelDto } from "@/shared/models/ai-models-dto";
import { gigabytes, percentOf } from "@/features/ai/ai-format";
import AiModelOptionComponent from "@/features/ai/components/AiModelOptionComponent";
import { aiKeys, useAiModels } from "@/features/ai/hooks/use-ai";
import { useAiModelDownload } from "@/features/ai/hooks/use-ai-model-download";
import { useLocalModel } from "@/features/ai/hooks/use-local-model";
import { useAppSettingMutations } from "@/features/settings/hooks/use-app-settings";
import { MONO } from "@/features/repository/repository-format";
import HomeCardComponent from "@/features/home/components/HomeCardComponent";
import HomeLinkComponent from "@/features/home/components/HomeLinkComponent";
import { MUTED } from "@/features/home/home-format";

const ICON = "microchip-ai";
const TITLE = "Local model";

const PILLS: Record<AiLocalModelStateEnum, { text: string; severity: StatusPillSeverity }> = {
  NOT_ANSWERING: { text: "Ollama not answering", severity: "danger" },
  NOT_CHOSEN: { text: "No model chosen", severity: "warning" },
  NOT_DOWNLOADED: { text: "Not downloaded", severity: "warning" },
  NOT_IN_MEMORY: { text: "Not in memory", severity: "neutral" },
  IN_MEMORY: { text: "In memory", severity: "success" },
};

const noteOf = (status: AiLocalModelStatusDto): string => {
  switch (status.state) {
    case AiLocalModelStateEnum.NOT_ANSWERING:
      return "Every AI feature is off until Ollama is running.";
    case AiLocalModelStateEnum.NOT_CHOSEN:
      return "Every AI feature is off until a model is chosen.";
    case AiLocalModelStateEnum.NOT_DOWNLOADED:
      return "Every AI feature is off: Ollama no longer has this model.";
    case AiLocalModelStateEnum.NOT_IN_MEMORY:
      return "Ollama loads it the next time something asks.";
    default:
      return `Using ${gigabytes(status.memoryBytes)} of memory.`;
  }
};

/**
 * Always the local model: every AI feature is off without it, whatever cloud model answers in its
 * place. The list asks Ollama about each model, so it is only read once it has been opened.
 */
export default function LocalModelCardComponent() {
  const queryClient = useQueryClient();
  const { setSettingMutation } = useAppSettingMutations();
  const { status, isFreeing, isStillInUse, free } = useLocalModel();
  const { progress, isDownloading } = useAiModelDownload();

  const [hasOpenedList, setHasOpenedList] = useState(false);
  const { data: available, isFetching } = useAiModels(hasOpenedList);

  if (!status)
    return (
      <HomeCardComponent
        icon={ICON}
        title={TITLE}
      >
        <Skeleton height="2.75rem" />
        <Skeleton height="2rem" />
      </HomeCardComponent>
    );

  const choose = (model: string) =>
    setSettingMutation.mutate(
      { key: AppSettingKeyEnum.AI_OLLAMA_MODEL, value: model },
      { onSuccess: () => queryClient.invalidateQueries({ queryKey: aiKeys.all }) },
    );

  let pill = (
    <StatusPillComponent
      text={PILLS[status.state].text}
      severity={PILLS[status.state].severity}
    />
  );
  if (isDownloading)
    pill = (
      <StatusPillComponent
        text="Downloading"
        severity="running"
        pulse
      />
    );
  if (isFreeing)
    pill = (
      <StatusPillComponent
        text="Freeing memory"
        severity="running"
        pulse
      />
    );

  return (
    <HomeCardComponent
      icon={ICON}
      title={TITLE}
      action={pill}
    >
      <Dropdown
        value={status.model}
        options={available?.models ?? []}
        optionLabel="name"
        optionValue="name"
        optionDisabled={(option: AiModelDto) => option.unusableReason !== ""}
        itemTemplate={(option: AiModelDto) => <AiModelOptionComponent model={option} />}
        // The list is empty until it is opened, and a model no longer downloaded is never in it.
        placeholder={status.model || "Choose a model"}
        emptyMessage={isFetching ? "Looking..." : "Nothing to choose from"}
        onShow={() => setHasOpenedList(true)}
        onChange={(e) => choose(e.value)}
        disabled={isFreeing}
        aria-label="Local model"
        className="w-full"
      />

      {isDownloading && progress && (
        <div className="flex flex-column gap-1">
          <span
            className="text-xs"
            style={MUTED}
          >
            Downloading {progress.model}
            {progress.total > 0 && ` · ${gigabytes(progress.completed)} of ${gigabytes(progress.total)}`}
          </span>
          <ProgressBar
            value={percentOf(progress.completed, progress.total)}
            mode={progress.total > 0 ? "determinate" : "indeterminate"}
            showValue={false}
            style={{ height: 6 }}
          />
        </div>
      )}

      <div className="flex align-items-center justify-content-between flex-wrap gap-2">
        <span
          className="text-sm"
          style={MUTED}
        >
          {noteOf(status)}
        </span>

        <Button
          type="button"
          label={isFreeing ? "Freeing..." : "Free memory"}
          icon="pi pi-eject"
          size="small"
          severity="secondary"
          outlined
          className="ml-auto"
          loading={isFreeing}
          disabled={status.state !== AiLocalModelStateEnum.IN_MEMORY}
          onClick={free}
        />
      </div>

      {isStillInUse && (
        <span
          className="text-xs"
          style={{ color: "var(--yellow-500)" }}
        >
          Still in use. Try again when it has finished.
        </span>
      )}

      {status.cloudModel && (
        <div
          className="flex align-items-center gap-2 p-2 border-round-lg text-xs"
          style={{ background: "var(--surface-ground)", ...MUTED }}
        >
          <i
            className="pi pi-cloud"
            aria-hidden="true"
          />
          <span>
            <span style={{ fontFamily: MONO }}>{status.cloudModel}</span> answers in the cloud. This model still reads
            the screens.
          </span>
        </div>
      )}

      <div className="flex justify-content-end">
        <HomeLinkComponent
          to="/settings/ai"
          label="AI settings"
        />
      </div>
    </HomeCardComponent>
  );
}
