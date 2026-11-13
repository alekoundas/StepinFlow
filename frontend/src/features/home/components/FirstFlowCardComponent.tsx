import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import { useRecordingStore } from "@/features/recording/store/recording-store";
import HomeHeroComponent from "@/features/home/components/HomeHeroComponent";

/** A new install has nothing to execute or fix yet, so Home leads with how to get a first flow. */
export default function FirstFlowCardComponent() {
  const navigate = useNavigate();
  const { setTarget, setCreateAsSubFlow } = useRecordingStore();

  const record = () => {
    setTarget(undefined);
    setCreateAsSubFlow(false);
    navigate("/record");
  };

  return (
    <HomeHeroComponent
      eyebrow="Welcome to StepinFlow"
      title="Start with your first flow"
      description="Build it step by step, do it once while StepinFlow records, or import a flow someone already wrote."
    >
      <Button
        type="button"
        label="Build a flow"
        icon="pi pi-pencil"
        size="large"
        onClick={() => navigate("/flows/new")}
      />
      <Button
        type="button"
        label="Record a flow (Beta)"
        icon="pi pi-video"
        size="large"
        outlined
        onClick={record}
      />
      <Button
        type="button"
        label="Import a flow"
        icon="pi pi-file-import"
        size="large"
        outlined
        onClick={() => navigate("/import")}
      />
    </HomeHeroComponent>
  );
}
