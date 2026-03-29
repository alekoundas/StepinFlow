import type { SelectItem } from "primereact/selectitem";
import {
  SelectButton,
  type SelectButtonChangeEvent,
} from "primereact/selectbutton";
import IconComponent from "@/shared/components/IconComponent";

export interface SelectButtonItem {
  label?: string;
  value: string;
  size?: "sm" | "base" | "lg";
  iconName?: string;
  disabled?: boolean;
}

interface Props {
  value: string;
  options: SelectButtonItem[];
  onChange: (value: string) => void;
}

export function SelectButtonComponent({ value, options, onChange }: Props) {
  const selectItems: SelectItem[] = options.map((item) => ({
    label: item.label,
    value: item.value,
    disabled: item.disabled,
    className: "p-1",
  }));

  const itemTemplate = (option: SelectItem) => {
    const matchingItem = options.find((it) => it.value === option.value);
    if (!matchingItem) return <></>;

    return (
      <IconComponent
        name={matchingItem.iconName ?? ""}
        size={matchingItem.size ?? "sm"}
      />
    );
  };

  return (
    <SelectButton
      value={value}
      onChange={(e: SelectButtonChangeEvent) => onChange(e.value)}
      options={selectItems}
      itemTemplate={itemTemplate}
    />
  );
}
