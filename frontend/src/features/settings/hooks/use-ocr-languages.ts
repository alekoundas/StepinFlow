import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { flowHealthKeys } from "@/features/flow/hooks/use-flow-health";
import { backendApiService } from "@/shared/services/backend-api-service";

export const ocrLanguageKeys = {
  all: ["lookup", "ocrLanguages"] as const,
} as const;

/** What a step that names no reader reads with, as the backend's OcrReaderCatalog.DEFAULT_READER. */
export const DEFAULT_OCR_READER = "latin";

export function useOcrLanguages() {
  return useQuery({
    queryKey: ocrLanguageKeys.all,
    queryFn: () => backendApiService.Lookup.ocrLanguages(),
  });
}

export function useOcrLanguageMutations() {
  const queryClient = useQueryClient();

  const downloadLanguageMutation = useMutation({
    mutationFn: (tag: string) => backendApiService.System.downloadOcrLanguage(tag),
    // A step whose reader was missing is no longer an error.
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ocrLanguageKeys.all }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
        queryClient.invalidateQueries({ queryKey: flowHealthKeys.all }),
      ]),
  });

  return { downloadLanguageMutation };
}
