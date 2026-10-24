import { Button } from "primereact/button";

import { TREE_ZOOM, useWorkflowStore } from "@/features/workflow/store/workflow-store";

/** − 100% + beside the tabs: draws the tree smaller or larger. The percentage puts it back. */
export function WorkflowTreeZoomComponent() {
  const treeZoom = useWorkflowStore((x) => x.treeZoom);
  const setTreeZoom = useWorkflowStore((x) => x.setTreeZoom);

  return (
    <div
      role="group"
      aria-label="Step size"
      className="flex align-items-center"
    >
      <Button
        type="button"
        icon="pi pi-minus"
        size="small"
        text
        aria-label="Smaller steps"
        disabled={treeZoom <= TREE_ZOOM.min}
        onClick={() => setTreeZoom(treeZoom - TREE_ZOOM.step)}
      />
      <Button
        type="button"
        label={`${treeZoom}%`}
        size="small"
        text
        severity="secondary"
        tooltip={`Back to ${TREE_ZOOM.initial}%`}
        tooltipOptions={{ position: "bottom" }}
        className="justify-content-center px-1"
        style={{ minWidth: "3.5rem", fontFamily: "monospace" }}
        onClick={() => setTreeZoom(TREE_ZOOM.initial)}
      />
      <Button
        type="button"
        icon="pi pi-plus"
        size="small"
        text
        aria-label="Larger steps"
        disabled={treeZoom >= TREE_ZOOM.max}
        onClick={() => setTreeZoom(treeZoom + TREE_ZOOM.step)}
      />
    </div>
  );
}
