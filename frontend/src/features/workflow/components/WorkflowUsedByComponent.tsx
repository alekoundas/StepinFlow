import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { flowStepAccent, getFlowStepCatalogEntry } from "@/shared/models/flow-step-catalog";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";

interface Props {
  steps: FlowStepUseDto[];
  emptyText: string;
}

/** The steps a part reaches. Each one opens in the Steps tab, its branches expanded down to it. */
export function WorkflowUsedByComponent({ steps, emptyText }: Props) {
  const setStepToReveal = useWorkflowStore((x) => x.setStepToReveal);

  return (
    <div className="flex flex-column gap-2 mt-4">
      <LabelComponent
        text="Used by"
        weight="bold"
      />

      {steps.length === 0 ? (
        <LabelComponent
          text={emptyText}
          size="sm"
          color="secondary"
        />
      ) : (
        <div className="flex flex-wrap gap-2">
          {steps.map((step) => (
            <Button
              key={step.flowStepId}
              type="button"
              label={step.name}
              icon={`pi pi-${getFlowStepCatalogEntry(step.flowStepType)?.iconName ?? "circle"}`}
              size="small"
              outlined
              severity="secondary"
              onClick={() => setStepToReveal(step.flowStepId)}
              pt={{ icon: { style: { color: flowStepAccent(step.flowStepType) } } }}
            />
          ))}
        </div>
      )}
    </div>
  );
}
