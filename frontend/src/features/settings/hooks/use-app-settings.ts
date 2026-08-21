import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";

export const appSettingKeys = {
  all: ["settings"] as const,
} as const;

export function useAppSettings() {
  return useQuery({
    queryKey: appSettingKeys.all,
    queryFn: () => backendApiService.Settings.getAll(),
  });
}

export function useAppSettingMutations() {
  const queryClient = useQueryClient();

  const setSettingMutation = useMutation({
    mutationFn: ({ key, value }: { key: AppSettingKeyEnum; value: string }) =>
      backendApiService.Settings.set(key, value),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: appSettingKeys.all }),
  });

  return { setSettingMutation };
}
