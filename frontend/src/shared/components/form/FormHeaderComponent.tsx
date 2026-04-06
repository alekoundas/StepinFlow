import LabelComponent from "@/shared/components/LabelComponent";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import { Button } from "primereact/button";

interface Props {
  formMode: FormMode;
  title: string;
  description: string;
  onEdit: () => void;
}

export function FormHeaderComponent({
  formMode,
  title,
  description,
  onEdit,
}: Props) {
  return (
    <div className="flex justify-content-between">
      <div>
        <LabelComponent
          text={title}
          size="lg"
          weight="bold"
        />
        <LabelComponent
          text={description}
          size="xs"
          className="mt-1"
        />
      </div>
      <Button
        icon="pi pi-pencil"
        label="Edit"
        className="p-button-outlined p-button-secondary"
        visible={formMode === "VIEW"}
        onClick={onEdit}
      />
    </div>
  );
}
