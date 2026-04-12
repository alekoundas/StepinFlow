import type { FormMode } from "@/shared/enums/form-mode-enum";
import { Button } from "primereact/button";

interface Props {
  formMode: FormMode;
  isValid: boolean;
  isDirty?: boolean;
  onCancel: () => void;
}

export function FormFooterComponent({
  formMode,
  isValid,
  isDirty,
  onCancel,
}: Props) {
  return (
    <div className="flex justify-end gap-3 mt-8">
      <Button
        type="button"
        label="Cancel"
        severity="secondary"
        visible={formMode === "ADD" || formMode === "EDIT"}
        onClick={onCancel}
      />
      <Button
        type="submit"
        label="Save"
        icon="pi pi-check"
        visible={formMode === "ADD" || formMode === "EDIT"}
        disabled={formMode === "VIEW" || !isValid || !isDirty}
        onClick={() =>
          console.log(
            "Save clicked – formMode:",
            formMode,
            "isValid:",
            isValid,
            "isDirty:",
            isDirty,
          )
        }
      />
    </div>
  );
}
