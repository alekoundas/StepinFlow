import { classNames } from "primereact/utils";

interface IParameters {
  text: string;
  className?: string;
  hidden?: boolean;
  wrap?: boolean;
  weight?: "light" | "normal" | "medium" | "semibold" | "bold";
  size?: "xs" | "sm" | "base" | "lg" | "xl";
}

export default function LabelComponent({
  text,
  className,
  hidden = false,
  wrap = false,
  weight = "normal",
  size = "base",
}: IParameters) {
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
