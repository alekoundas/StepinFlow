import { useQuery } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { CommandPresetDto } from "@/shared/models/database/command-preset-dto";

// The catalog is the backend's, so the preview and the runner build the command the same way.
export function useCommandPresets() {
  return useQuery<CommandPresetDto[]>({
    queryKey: ["lookup", "commandPresets"],
    queryFn: () => backendApiService.Lookup.commandPresets(),
    staleTime: Infinity,
  });
}
