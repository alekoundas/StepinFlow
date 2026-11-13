import { Button } from "primereact/button";

import { useWorkflowStore } from "@/features/workflow/store/workflow-store";

/** Expand all and Collapse all above the tree. The tree does the work: it holds which rows are open. */
export function WorkflowTreeExpansionComponent() {
  const setTreeExpansion = useWorkflowStore((x) => x.setTreeExpansion);

  return (
    <div
      role="group"
      aria-label="Open steps"
      className="flex align-items-center"
    >
      <Button
        type="button"
        icon="pi pi-angle-double-down"
        size="small"
        text
        aria-label="Expand all"
        tooltip="Expand all"
        tooltipOptions={{ position: "bottom" }}
        onClick={() => setTreeExpansion("expand")}
      />
      <Button
        type="button"
        icon="pi pi-angle-double-up"
        size="small"
        text
        aria-label="Collapse all"
        tooltip="Collapse all"
        tooltipOptions={{ position: "bottom" }}
        onClick={() => setTreeExpansion("collapse")}
      />
    </div>
  );
}
