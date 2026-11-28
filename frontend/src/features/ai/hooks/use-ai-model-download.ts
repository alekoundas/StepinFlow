import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { BackendApiService } from "@/shared/services/backend-api-service";
import { AiHooks } from "@/features/ai/hooks/use-ai";
import type { AiModelDownloadEventDto } from "@/shared/models/ai-model-download-event-dto";

/**
 * Downloads a model and follows it.
 *
 * The backend owns the state, not this hook. A download runs for minutes and outlives whatever was
 * on screen when it started, so opening Settings later - or closing and reopening this panel - has
 * to show what is actually happening rather than nothing.
 *
 * Broadcasts keep it smooth, the poll keeps it correct: a missed message costs a second of
 * staleness instead of a page that has lost the download entirely.
 */
export function useAiModelDownload() {
  const queryClient = useQueryClient();

  const { data: progress } = useQuery({
    queryKey: AiHooks.keys.downloadState(),
    queryFn: () => BackendApiService.Ai.getDownloadState(),
    // Only while something is running. A finished one changes when somebody presses a button.
    refetchInterval: (query) => {
      const current = query.state.data as AiModelDownloadEventDto | null | undefined;
      return current && !current.isDone ? 1000 : false;
    },
  });

  useEffect(() => {
    const unsubscribe = ElectronApiService.BackendApi.onBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.AI_MODEL_DOWNLOAD_EVENT) return;

      const event = message.payload as AiModelDownloadEventDto;
      queryClient.setQueryData(AiHooks.keys.downloadState(), event);

      // Finished, so what is installed has changed and the model list is stale. The backend
      // forgets a finished download, so asking again clears the panel rather than leaving a banner.
      if (event.isDone && !event.error) {
        queryClient.invalidateQueries({ queryKey: AiHooks.keys.models() });
        queryClient.invalidateQueries({ queryKey: AiHooks.keys.suggestions() });
        queryClient.invalidateQueries({ queryKey: AiHooks.keys.status() });
        queryClient.invalidateQueries({ queryKey: AiHooks.keys.localModel() });
        queryClient.invalidateQueries({ queryKey: AiHooks.keys.downloadState() });
      }
    });

    return () => {
      unsubscribe?.();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const download = async (model: string) => {
    try {
      await BackendApiService.Ai.downloadModel(model);
    } catch (error) {
      queryClient.setQueryData(AiHooks.keys.downloadState(), {
        model: model,
        status: "",
        completed: 0,
        total: 0,
        isDone: true,
        error:
          error instanceof Error
            ? error.message
            : "The download could not be started.",
      } satisfies AiModelDownloadEventDto);
      return;
    }

    queryClient.invalidateQueries({ queryKey: AiHooks.keys.downloadState() });
  };

  // Only a finished one can be dismissed. Hiding a running download would be a lie - the file
  // keeps growing either way, and the backend refuses.
  const dismiss = async () => {
    await BackendApiService.Ai.clearDownloadState();
    queryClient.invalidateQueries({ queryKey: AiHooks.keys.downloadState() });
  };

  const isDownloading = !!progress && !progress.isDone;

  return { progress: progress ?? undefined, isDownloading, download, dismiss };
}
