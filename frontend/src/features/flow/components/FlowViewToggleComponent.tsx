import { Button } from "primereact/button";

type Props = {
  mode: "table" | "cards";
  onChange: (mode: "table" | "cards") => void;
};

export function FlowViewToggleComponent({ mode, onChange }: Props) {
  return (
    <div className="flex gap-1">
      <Button
        icon="pi pi-table"
        severity={mode === "table" ? "info" : "secondary"}
        onClick={() => onChange("table")}
        tooltip="Table View"
      />
      <Button
        icon="pi pi-th-large"
        severity={mode === "cards" ? "info" : "secondary"}
        onClick={() => onChange("cards")}
        tooltip="Card View"
      />
    </div>
  );
}
