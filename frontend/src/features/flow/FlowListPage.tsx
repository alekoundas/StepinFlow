import { useState } from "react";
import { useNavigate } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { FlowViewToggleComponent } from "@/features/flow/components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "@/features/flow/components/FlowDataTableComponent";
import { FlowDataGridComponent } from "@/features/flow/components/FlowDataGridComponent";
import { useWizardStore } from "@/features/wizard/store/wizard-store";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FlowCreateChoiceDialogComponent } from "@/features/flow/components/FlowCreateChoiceDialogComponent";

interface Props {
  /** Sub-flows are the same page with the flag flipped: same table, same recorder, same editor. */
  isSubFlow?: boolean;
}

export default function FlowListPage({ isSubFlow = false }: Props) {
  const navigate = useNavigate();
  const { setTarget, setCreateAsSubFlow } = useWizardStore();
  const { openCustom } = useDialogStore();
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");

  const noun = isSubFlow ? "Sub-Flow" : "Flow";

  const handleNew = () => navigate("/flows/new");

  // Recording was a button of its own next to New, which read as a different kind of thing rather
  // than another way of doing the same thing. All three start here now.
  const handleCreate = () => {
    openCustom(
      "flow-create-choice",
      <FlowCreateChoiceDialogComponent
        noun={noun}
        onManual={isSubFlow ? undefined : handleNew}
        onRecord={handleRecord}
        onAi={handleCreateWithAi}
      />,
    );
  };

  const handleCreateWithAi = () => {
    setTarget(undefined);
    setCreateAsSubFlow(isSubFlow);
    navigate("/flows/ai");
  };

  // The recorder names and creates the flow itself, so its step forms have somewhere to list
  // search areas and points from.
  const handleRecord = () => {
    setTarget(undefined);
    setCreateAsSubFlow(isSubFlow);
    navigate("/record");
  };

  return (
    <div className="m-4">
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text={isSubFlow ? "Sub-Flows" : "Flows"}
          size="5xl"
          weight="bold"
        />

        <Button
          label={`New ${noun}`}
          icon="pi pi-plus"
          onClick={handleCreate}
        />
      </div>

      <Card className="mt-6">
        <div className="flex flex-wrap justify-content-between items-center">
          <div className="flex flex-column">
            <LabelComponent
              text={isSubFlow ? "Available Sub-Flows" : "Available Flows"}
              size="lg"
              weight="bold"
            />
            <LabelComponent
              text={
                isSubFlow
                  ? "Flows meant to be run from another flow rather than started on their own."
                  : "Everything you can run."
              }
              size="xs"
            />
          </div>

          <FlowViewToggleComponent
            mode={viewMode}
            onChange={setViewMode}
          />
        </div>

        {viewMode === "table" ? (
          <FlowDataTableComponent
            className="mt-4"
            isSubFlow={isSubFlow}
          />
        ) : (
          <FlowDataGridComponent
            className="mt-4"
            isSubFlow={isSubFlow}
          />
        )}
      </Card>
    </div>
  );
}
