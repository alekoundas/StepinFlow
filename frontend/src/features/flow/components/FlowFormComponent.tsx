import type { FlowDto } from "@/shared/models/database/flow/flow-dto";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useState } from "react";
import { InputNumber } from "primereact/inputnumber";
import { FormMode } from "@/shared/enums/form-mode-enum";

type Props = {
  formMode: FormMode;
  initialData?: Partial<FlowDto>;
  onSubmit: (data: FlowDto) => void;
  onCancel: () => void;
  loading?: boolean;
};

export function FlowFormComponent({
  formMode,
  initialData = {},
  onSubmit,
  onCancel,
  loading,
}: Props) {
  const [name, setName] = useState(initialData.name ?? "");
  const [orderNumber, setOrderNumber] = useState(initialData.orderNumber ?? 0);

  const isView = formMode === FormMode.VIEW;

  const handleSubmit = () => {
    const data = { name, orderNumber };
    onSubmit(data as any);
  };

  return (
    <div className="p-6 max-w-lg mx-auto">
      <div className="flex flex-col gap-4">
        <div>
          <label>Name</label>
          <InputText
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={isView}
            className="w-full"
          />
        </div>
        <div>
          <label>Order Number</label>
          <InputNumber
            value={orderNumber}
            onChange={(e) => setOrderNumber(Number(e.value))}
            disabled={isView}
            className="w-full"
          />
        </div>
      </div>

      <div className="flex justify-end gap-3 mt-8">
        <Button
          label="Cancel"
          severity="secondary"
          onClick={onCancel}
        />
        {!isView && (
          <Button
            label="Save"
            onClick={handleSubmit}
            loading={loading}
          />
        )}
      </div>
    </div>
  );
}
