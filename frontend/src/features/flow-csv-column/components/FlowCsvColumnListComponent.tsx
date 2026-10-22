import type { FlowCsvColumnDto } from "@/shared/models/database/flow-csv-column-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import { typesInput } from "@/features/workflow/hooks/use-flow-parts";
import {
  WorkflowPartListComponent,
  type WorkflowPartSelection,
} from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  inputs: FlowCsvColumnDto[];
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

/** The Inputs tab's list: each value the steps type in. */
export function FlowCsvColumnListComponent({ inputs, uses, selected, onSelect }: Props) {
  const items = inputs.map((input) => ({
    id: input.id,
    icon: input.isSecret ? "lock" : "pencil",
    name: input.name,
    detail: input.isSecret ? "secret" : input.defaultValue ? `default ${input.defaultValue}` : "no default",
    usedCount: (uses ?? []).filter((x) => typesInput(x, input.name)).length,
  }));

  return (
    <WorkflowPartListComponent
      note="Values the steps type in, like the email to sign in with."
      addLabel="Add input"
      emptyText="No inputs yet."
      items={items}
      selected={selected}
      onSelect={onSelect}
    />
  );
}
