import { backendApiService } from "@/shared/services/backend-api-service";
import { useQuery } from "@tanstack/react-query";

export const flowKeys = {
  detail: (id: number) => ["flow", "detail", id] as const,
} as const;

export function useFlow(id: number | null) {
  return useQuery({
    queryKey: id ? flowKeys.detail(id) : ["flow", "detail", "disabled"],
    queryFn: () => backendApiService.Flow.get(id!),
    enabled: !!id,
    // staleTime: 5 * 60 * 1000,         // override from global if needed
  });
}
