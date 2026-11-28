import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import { BackendApiService } from "@/shared/services/backend-api-service";
import { useQuery } from "@tanstack/react-query";

const flowKeys = {
  list: (params: LazyDto) => ["flows", "list", params] as const,
} as const;

export function useFlows(lazyParams: LazyDto) {
  return useQuery({
    queryKey: flowKeys.list(lazyParams),
    queryFn: () => BackendApiService.Flow.getLazy(lazyParams),
    staleTime: 2 * 60 * 1000, // 2 minutes – flows list changes less often than single items
  });
}
