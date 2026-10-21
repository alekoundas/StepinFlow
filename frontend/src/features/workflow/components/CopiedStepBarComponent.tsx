import { Button } from "primereact/button";

import IconComponent from "@/shared/components/IconComponent";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";

/** Says what Copy took, which is why every + has a Paste beside it, and lets go of it. */
export function CopiedStepBarComponent() {
  const { copiedStep, setCopiedStep } = useWorkflowStore();

  if (!copiedStep) return null;

  return (
    <div
      className="flex align-items-center gap-2 px-3 py-2 mx-3 mt-3 border-round border-1 surface-border text-sm"
      style={{ background: "color-mix(in srgb, var(--primary-color) 10%, transparent)" }}
    >
      <IconComponent
        name="copy"
        size="sm"
      />
      <span className="flex-1 min-w-0">
        Copied <span className="font-semibold">{copiedStep.name}</span> and everything under it.
        Paste it beside any + in this flow.
      </span>
      <Button
        type="button"
        icon="pi pi-times"
        size="small"
        text
        rounded
        aria-label="Forget the copy"
        tooltip="Forget the copy"
        tooltipOptions={{ position: "left" }}
        onClick={() => setCopiedStep(undefined)}
      />
    </div>
  );
}
