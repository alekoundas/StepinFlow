import { classNames } from "primereact/utils";

interface IParameters {
  name: string;
  size?: "xs" | "sm" | "base" | "lg" | "lg2";
  className?: string;
}

export default function IconComponent({
  name,
  size = "base",
  className,
}: IParameters) {
  const fontSizeMap = {
    xs: "0.5rem",
    sm: "1rem",
    base: "1.5rem",
    lg: "2rem",
    lg2: "2.5rem",
  };

  return (
    <>
      <i
        className={classNames(`pi pi-${name}`, className)}
        style={{ fontSize: fontSizeMap[size] }}
      />
    </>
  );
}
