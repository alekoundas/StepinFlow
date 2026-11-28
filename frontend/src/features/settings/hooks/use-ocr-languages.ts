import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FlowHealthHooks } from "@/features/flow/hooks/use-flow-health";
import { BackendApiService } from "@/shared/services/backend-api-service";

const ocrLanguageKeys = {
  all: ["lookup", "ocrLanguages"] as const,
} as const;

/** What a step that names no reader reads with, as the backend's OcrReaderCatalog.DEFAULT_READER. */
const DEFAULT_OCR_READER = "latin";

function useOcrLanguages() {
  return useQuery({
    queryKey: ocrLanguageKeys.all,
    queryFn: () => BackendApiService.Lookup.ocrLanguages(),
  });
}

function useOcrLanguageMutations() {
  const queryClient = useQueryClient();

  const downloadLanguageMutation = useMutation({
    mutationFn: (tag: string) => BackendApiService.System.downloadOcrLanguage(tag),
    // A step whose reader was missing is no longer an error.
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ocrLanguageKeys.all }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
        queryClient.invalidateQueries({ queryKey: FlowHealthHooks.keys.all }),
      ]),
  });

  return { downloadLanguageMutation };
}

export const OcrLanguageHooks = {
  DEFAULT_OCR_READER,
  useOcrLanguages,
  useOcrLanguageMutations,
};
