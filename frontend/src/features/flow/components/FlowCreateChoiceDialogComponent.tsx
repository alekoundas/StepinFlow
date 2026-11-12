import { Dialog } from "primereact/dialog";

import LabelComponent from "@/shared/components/LabelComponent";
import { ChoiceCardComponent } from "@/shared/components/ChoiceCardComponent";

export interface IFlowCreateChoiceDialogProps {
  onManual: () => void;
  onRecord: () => void;

  onClose?: () => void; // injected by DialogRootComponent
}

/**
 * How to start a flow.
 *
 * The two ways used to be two buttons on the page, which made the recorder look like an
 * afterthought next to New rather than the other half of the same decision.
 */
export function FlowCreateChoiceDialogComponent({
  onManual,
  onRecord,
  onClose,
}: IFlowCreateChoiceDialogProps) {
  const choose = (act: () => void) => {
    onClose?.();
    act();
  };

  return (
    <Dialog
      visible
      header="New Flow"
      onHide={() => onClose?.()}
      style={{ width: "38rem" }}
      draggable={false}
    >
      <div className="flex flex-column gap-3">
        <LabelComponent
          text="Two ways to get to the same place. Both end in the editor."
          size="sm"
          color="secondary"
        />

        <ChoiceCardComponent
          icon="pencil"
          title="Build it myself"
          description="Start with an empty flow and add steps one at a time."
          onSelect={() => choose(onManual)}
        />

        <ChoiceCardComponent
          icon="circle-fill"
          title="Record it"
          tag="Beta"
          description="Do the task once. Every click and keystroke is captured, and you say what each one should become."
          onSelect={() => choose(onRecord)}
        />
      </div>
    </Dialog>
  );
}

