interface IParameters {
  name: string;
  size?: "sm" | "base" | "lg" | "lg2";
}

export default function IconComponent(parameters: IParameters) {
  const fontSizeMap = {
    sm: "1rem",
    base: "1.5rem",
    lg: "2rem",
    lg2: "2.5rem",
  };

  return (
    <>
      <i
        className={`pi pi-${parameters.name}`}
        style={{ fontSize: fontSizeMap[parameters.size ?? "base"] }}
      />
    </>
  );
}
