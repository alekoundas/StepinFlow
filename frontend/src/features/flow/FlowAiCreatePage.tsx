import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";

/**
 * Create a flow with the assistant.
 *
 * A placeholder for now: the entry point exists so the choice on the flows page is real, and what
 * goes here is built behind it. Nothing is generated yet.
 */
export default function FlowAiCreatePage() {
  const navigate = useNavigate();

  return (
    <div className="flex flex-column gap-3 p-4">
      <LabelComponent
        text="Create with AI"
        size="2xl"
        weight="bold"
      />

      <LabelComponent
        text="Not built yet. Describe the task, record it once, or both, and the steps get written for you - then land in the editor to check before anything runs."
        size="sm"
        color="secondary"
      />

      <div className="flex gap-2 mt-2">
        <Button
          label="Back to flows"
          icon="pi pi-arrow-left"
          text
          onClick={() => navigate("/flows")}
        />
        <Button
          label="Record"
          icon="pi pi-circle-fill"
          outlined
          onClick={() => navigate("/record")}
        />
      </div>
    </div>
  );
}
