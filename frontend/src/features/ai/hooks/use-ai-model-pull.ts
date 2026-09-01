import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { backendApiService } from "@/shared/services/backend-api-service";
import { aiKeys } from "@/features/ai/hooks/use-ai";
import type { AiModelPullEventDto } from "@/shared/models/ai-model-pull-event-dto";

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
export function useAiModelPull() {
  const queryClient = useQueryClient();

  const { data: progress } = useQuery({
    queryKey: aiKeys.pullState(),
    queryFn: () => backendApiService.Ai.getPullState(),
    // Only while something is running. A finished one changes when somebody presses a button.
    refetchInterval: (query) => {
      const current = query.state.data as AiModelPullEventDto | null | undefined;
      return current && !current.isDone ? 1000 : false;
    },
  });

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.AI_MODEL_PULL_EVENT) return;

      const event = message.payload as AiModelPullEventDto;
      queryClient.setQueryData(aiKeys.pullState(), event);

      // Finished, so what is installed has changed and the model list is stale. The backend
      // forgets a successful pull, so asking again clears the panel rather than leaving a banner.
      if (event.isDone && !event.error) {
        queryClient.invalidateQueries({ queryKey: aiKeys.models() });
        queryClient.invalidateQueries({ queryKey: aiKeys.suggestions() });
        queryClient.invalidateQueries({ queryKey: aiKeys.status() });
        queryClient.invalidateQueries({ queryKey: aiKeys.pullState() });
      }
    });

    return () => {
      unsubscribe?.();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const pull = async (model: string) => {
    try {
      await backendApiService.Ai.pullModel(model);
    } catch (error) {
      queryClient.setQueryData(aiKeys.pullState(), {
        model: model,
        status: "",
        completed: 0,
        total: 0,
        isDone: true,
        error:
          error instanceof Error
            ? error.message
            : "The download could not be started.",
      } satisfies AiModelPullEventDto);
      return;
    }

    queryClient.invalidateQueries({ queryKey: aiKeys.pullState() });
  };

  // Only a finished one can be dismissed. Hiding a running download would be a lie - the file
  // keeps growing either way, and the backend refuses.
  const dismiss = async () => {
    await backendApiService.Ai.clearPullState();
    queryClient.invalidateQueries({ queryKey: aiKeys.pullState() });
  };

  const isPulling = !!progress && !progress.isDone;

  return { progress: progress ?? undefined, isPulling, pull, dismiss };
}
