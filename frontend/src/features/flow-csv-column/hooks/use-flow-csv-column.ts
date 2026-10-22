import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowCsvColumnDto } from "@/shared/models/database/flow-csv-column-dto";

// Mutation CRUD
export function useFlowCsvColumnMutations() {
  const queryClient = useQueryClient();

  // The inputs come with the flow, and a renamed one can leave a step typing a name nothing has.
  // Awaited, so a save resolves once the lists show it.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["flow"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

  const createFlowCsvColumnMutation = useMutation({
    mutationFn: (dto: FlowCsvColumnDto) => backendApiService.FlowCsvColumn.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowCsvColumnMutation = useMutation({
    mutationFn: (dto: FlowCsvColumnDto) => backendApiService.FlowCsvColumn.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowCsvColumnMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowCsvColumn.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowCsvColumnMutation,
    updateFlowCsvColumnMutation,
    deleteFlowCsvColumnMutation,
  };
}
