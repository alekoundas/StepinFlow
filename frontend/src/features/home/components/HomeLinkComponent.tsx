import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

/** A card's way to the page with the rest of what it shows. */
export default function HomeLinkComponent({ to, label }: { to: string; label: string }) {
  const navigate = useNavigate();

  return (
    <Button
      type="button"
      label={label}
      icon="pi pi-arrow-right"
      iconPos="right"
      text
      size="small"
      onClick={() => navigate(to)}
    />
  );
}
