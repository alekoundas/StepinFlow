import { useState } from "react";

import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";

interface Props {
  icon: string;
  title: string;
  description: string;
  onSelect: () => void;
  children?: React.ReactNode;
}

/** One answer in a dialog that asks how to do something, picked by clicking the whole card. */
export function ChoiceCardComponent({ icon, title, description, onSelect, children }: Props) {
  const [isHovered, setIsHovered] = useState(false);

  return (
    <div
      onClick={onSelect}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className="flex align-items-start gap-3 border-1 border-round p-3 cursor-pointer"
      style={{
        borderColor: isHovered ? "var(--primary-color)" : "var(--surface-border)",
        background: isHovered ? "var(--highlight-bg)" : "var(--surface-ground)",
      }}
    >
      <IconComponent
        name={icon}
        className={isHovered ? "text-primary" : "text-color-secondary"}
      />

      <div className="flex flex-column gap-1">
        <LabelComponent
          text={title}
          weight="semibold"
          color={isHovered ? "primary" : undefined}
        />
        <LabelComponent
          text={description}
          size="sm"
          color="secondary"
        />
        {children}
      </div>
    </div>
  );
}
