import { classNames } from "primereact/utils";

interface Props {
  text: string | number;
  className?: string;
  isRequired?: boolean;
  hidden?: boolean;
  wrap?: boolean;
  weight?: "normal" | "semibold" | "bold";
  size?:
    | "xs"
    | "sm"
    | "base"
    | "lg"
    | "xl"
    | "2xl"
    | "3xl"
    | "4xl"
    | "5xl"
    | "6xl"
    | "7xl"
    | "8xl";
  color?: "primary" | "secondary" | "error" | "success" | "warning" | "info";
}

export default function LabelComponent({
  text,
  className,
  hidden = false,
  wrap = true,
  weight = "normal",
  size = "base",
  isRequired = false,
  color = "primary",
}: Props) {
  const colorMap = {
    primary: "text-primary",
    secondary: "text-color-secondary",
    error: "p-error",
    success: "text-color-success",
    warning: "text-color-warning",
    info: "text-color-info",
  };
  return (
    <>
      <div className={colorMap[color]}>
        <p
          className={classNames(
            className,
            weight && `font-${weight}`,
            size && `text-${size}`,
            wrap ? "white-space-normal" : "white-space-nowrap",
            "m-0",
          )}
          hidden={hidden}
        >
          {text} {isRequired && <span className="p-error">*</span>}
        </p>
      </div>
    </>
  );
}
