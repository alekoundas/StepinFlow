import { classNames } from "primereact/utils";

interface Props {
  text: string | number;
  className?: string;
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
  isRequired?: boolean;
}

export default function LabelComponent({
  text,
  className,
  hidden = false,
  wrap = false,
  weight = "normal",
  size = "base",
  isRequired = false,
}: Props) {
  return (
    <>
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
        {text}
      </p>
    </>
  );
}
