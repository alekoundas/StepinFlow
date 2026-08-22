import { useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";

/**
 * Health for every flow, in one cached answer.
 *
 * Not part of the list query: validating one flow loads every step it has, so folding it in
 * would do that for every row before anything paints. The rows appear immediately and the badges
 * arrive a beat later.
 *
 * All flows rather than the current page, so nothing has to know which rows are on screen and
 * paging costs nothing. Fine at this size; page it if an install ever holds hundreds.
 */
export function useFlowHealth() {
  const { data } = useQuery({
    queryKey: ["flow", "health"],
    queryFn: () => backendApiService.Flow.getHealth([]),
  });

  return new Map<number, FlowHealthDto>((data ?? []).map((x) => [x.flowId, x]));
}
