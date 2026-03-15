import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useState, useEffect } from "react";
import { useFlowStore } from "./store/flow-store";
import { InputNumber } from "primereact/inputnumber";
import { FormMode } from "../../models/enums/form-mode-enum";

type Props = {
  visible: boolean;
  mode: FormMode;
  flowId?: number | null;
  onHide: () => void;
};

export function FlowFormComponent({ visible, mode, flowId, onHide }: Props) {
  const [name, setName] = useState("");
  const [orderNumber, setOrderNumber] = useState(0);

  const { loading } = useFlowStore();

  const isView = mode === FormMode.VIEW;
  const isEdit = mode === FormMode.EDIT || mode === FormMode.CLONE;

  useEffect(() => {
    if (flowId && (isEdit || isView)) {
      // TODO: load flow by id using useFlow hook
    }
  }, [flowId]);

  const handleSave = () => {
    // const dto = { name, orderNumber };

    if (mode === FormMode.ADD || mode === FormMode.CLONE) {
      // createFlow(dto, { onSuccess: onHide });
    } else if (mode === FormMode.EDIT && flowId) {
      // updateFlow({ id: flowId, dto }, { onSuccess: onHide });
    }
  };

  return (
    <Dialog
      header={mode === FormMode.ADD ? "New Flow" : "Edit Flow"}
      visible={visible}
      onHide={onHide}
      style={{ width: "500px" }}
    >
      <div className="flex flex-col gap-4 py-4">
        <div>
          <label>Name</label>
          <InputText
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={isView}
          />
        </div>
        <div>
          <label>Order Number</label>
          <InputNumber
            value={orderNumber}
            onChange={(e) => setOrderNumber(Number(e.value))}
            disabled={isView}
          />
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button
          label="Cancel"
          severity="secondary"
          onClick={onHide}
        />
        {!isView && (
          <Button
            label="Save"
            onClick={handleSave}
            loading={loading}
          />
        )}
      </div>
    </Dialog>
  );
}
