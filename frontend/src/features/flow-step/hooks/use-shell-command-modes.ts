import { useQuery } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { ShellCommandModeDto } from "@/shared/models/database/shell-command-mode-dto";

// The catalog is the backend's, so the preview and the executor build the command the same way.
export function useShellCommandModes() {
  return useQuery<ShellCommandModeDto[]>({
    queryKey: ["lookup", "shellCommandModes"],
    queryFn: () => backendApiService.Lookup.shellCommandModes(),
    staleTime: Infinity,
  });
}
