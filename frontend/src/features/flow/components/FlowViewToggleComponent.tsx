import {
  SelectButtonComponent,
  type SelectButtonItem,
} from "@/shared/components/SelectButtonComponent";

interface Props {
  mode: "table" | "cards";
  onChange: (mode: "table" | "cards") => void;
}

export function FlowViewToggleComponent({ mode, onChange }: Props) {
  const options: SelectButtonItem[] = [
    { value: "table", iconName: "list", size: "base" },
    { value: "cards", iconName: "objects-column", size: "base" },
  ];

  return (
    <SelectButtonComponent
      value={mode}
      onChange={(value: string) => {
        if (value === "table" || value === "cards") {
          onChange(value);
        }
      }}
      options={options}
    />
  );
}
