import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import { aiKeys } from "@/features/ai/hooks/use-ai";
import { AiLocalModelStateEnum } from "@/shared/enums/backend/ai/ai-local-model-state-enum";

// Ollama starts, stops and lets a model go without telling anybody.
const CHECK_EVERY_MILLISECONDS = 15_000;
const CHECK_WHILE_FREEING_MILLISECONDS = 1000;

// A job of several model calls loads the model again after each one, so it may never be seen gone.
const FREEING_LIMIT_MILLISECONDS = 120_000;

/**
 * Where the local model stands, and letting it go.
 *
 * Only the status says the model has gone. Ollama agrees at once, even while the model is busy, and
 * unloads it once it goes idle. The request itself can wait behind an answer on the pipe past
 * Electron's 30 seconds, and the backend still acts on it, so how it ends says nothing either.
 */
export function useLocalModel() {
  const [isFreeing, setIsFreeing] = useState(false);
  const [didRunOut, setDidRunOut] = useState(false);

  const { data: status } = useQuery({
    queryKey: aiKeys.localModel(),
    queryFn: () => backendApiService.Ai.getLocalModelStatus(),
    staleTime: 0,
    retry: false,
    refetchInterval: isFreeing ? CHECK_WHILE_FREEING_MILLISECONDS : CHECK_EVERY_MILLISECONDS,
  });

  const isInMemory = status?.state === AiLocalModelStateEnum.IN_MEMORY;

  if (isFreeing && status && !isInMemory) setIsFreeing(false);

  useEffect(() => {
    if (!isFreeing) return;

    const timer = setTimeout(() => {
      setIsFreeing(false);
      setDidRunOut(true);
    }, FREEING_LIMIT_MILLISECONDS);

    return () => clearTimeout(timer);
  }, [isFreeing]);

  const free = () => {
    setDidRunOut(false);
    setIsFreeing(true);
    void backendApiService.Ai.freeLocalModel().catch(() => undefined);
  };

  return { status, isFreeing, isStillInUse: didRunOut && isInMemory, free };
}
