import { useEffect, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

const DEBOUNCE_MILLISECONDS = 300;

/**
 * The line the step on a form will be in its flow's script, asked for once typing pauses. The
 * last line stays on screen while the next is fetched, so the preview never blinks empty.
 */
export function useFlowStepScriptLine(dto: FlowStepDto) {
  // The line needs a template's name, never its picture, and a keystroke should not send megabytes.
  const request = JSON.stringify({
    ...dto,
    flowStepTemplates: (dto.flowStepTemplates ?? []).map((x) => ({
      ...x,
      flowTemplate: { ...x.flowTemplate, templateImage: undefined },
    })),
  });

  const [debounced, setDebounced] = useState(request);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(request), DEBOUNCE_MILLISECONDS);
    return () => clearTimeout(timer);
  }, [request]);

  return useQuery({
    queryKey: ["flowStepScriptLine", debounced],
    queryFn: () => backendApiService.FlowStep.getScriptLine(JSON.parse(debounced) as FlowStepDto),
    placeholderData: keepPreviousData,
    retry: false,
  });
}
