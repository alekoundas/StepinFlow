import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";

export const ocrLanguageKeys = {
  all: ["lookup", "ocrLanguages"] as const,
} as const;

/**
 * A pack can take minutes to download and nothing tells us when it lands, so while one is
 * installing the list asks again instead.
 */
export function useOcrLanguages(isInstalling = false) {
  return useQuery({
    queryKey: ocrLanguageKeys.all,
    queryFn: () => backendApiService.Lookup.ocrLanguages(),
    refetchInterval: isInstalling ? 5000 : false,
  });
}

export function useOcrLanguageMutations() {
  const queryClient = useQueryClient();

  const installLanguageMutation = useMutation({
    mutationFn: (tag: string) => backendApiService.System.installOcrLanguage(tag),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ocrLanguageKeys.all }),
  });

  return { installLanguageMutation };
}
