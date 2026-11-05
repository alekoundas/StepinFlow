import { useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";

export const flowHealthKeys = {
  all: ["flow", "health"] as const,
} as const;

/**
 * Health for every flow, in one cached answer.
 *
 * Not part of the list query: validating one flow loads every step it has, so folding it in
 * would do that for every row before anything paints. The rows appear immediately and the badges
 * arrive a beat later.
 *
 * All flows rather than the current page, so nothing has to know which rows are on screen and
 * paging costs nothing. Fine at this size; page it if an install ever holds hundreds.
 *
 * Asked again on every page opened, and by useRepositoryEvents once any flow is saved, whichever
 * way it was saved.
 */
function useFlowHealthQuery() {
  return useQuery({
    queryKey: flowHealthKeys.all,
    queryFn: () => backendApiService.Flow.getHealth([]),
    staleTime: 0,
  });
}

export function useFlowHealth() {
  const { data } = useFlowHealthQuery();

  return new Map<number, FlowHealthDto>((data ?? []).map((x) => [x.flowId, x]));
}

/** The flows with an error or a warning, the most errors first. */
export function useFlowsWithIssues() {
  const { data, isPending } = useFlowHealthQuery();

  const flows = (data ?? [])
    .filter((x) => x.errorCount + x.warningCount > 0)
    .sort(
      (a, b) =>
        b.errorCount - a.errorCount ||
        b.warningCount - a.warningCount ||
        a.flowName.localeCompare(b.flowName),
    );

  return { flows, isLoading: isPending };
}
