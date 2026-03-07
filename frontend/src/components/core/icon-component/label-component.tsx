import { classNames } from "primereact/utils";

interface IParameters {
  text: string;
  weight?: "light" | "normal" | "medium" | "semibold" | "bold";
  size?: "xs" | "sm" | "base" | "lg" | "xl";
  className?: string;
}

export default function LabelComponent({
  text,
  weight = "normal",
  size = "base",
  className,
}: IParameters) {
  return (
    <>
      <p
        className={classNames(
          className,
          weight && `font-${weight}`,
          size && `text-${size}`,
        )}
      >
        {text}
      </p>
    </>
  );
}
