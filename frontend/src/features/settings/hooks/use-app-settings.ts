import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BackendApiService } from "@/shared/services/backend-api-service";
import type { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";

const appSettingKeys = {
  all: ["settings"] as const,
} as const;

function useAppSettings() {
  return useQuery({
    queryKey: appSettingKeys.all,
    queryFn: () => BackendApiService.Settings.getAll(),
  });
}

function useAppSettingMutations() {
  const queryClient = useQueryClient();

  const setSettingMutation = useMutation({
    mutationFn: ({ key, value }: { key: AppSettingKeyEnum; value: string }) =>
      BackendApiService.Settings.set(key, value),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: appSettingKeys.all }),
  });

  return { setSettingMutation };
}

export const AppSettingHooks = {
  useAppSettings,
  useAppSettingMutations,
};
