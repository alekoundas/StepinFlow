import { Tag } from "primereact/tag";

interface Props {
  count: number;
  singularNoun?: string;
  pluralNoun?: string;
}

// How many flow steps reference a reusable entry (search area, location).
// Warning severity when nothing uses it, so unused leftovers stand out.
export function UsageCountTagComponent({
  count,
  singularNoun = "step",
  pluralNoun = "steps",
}: Props) {
  return (
    <Tag
      value={`${count} ${count === 1 ? singularNoun : pluralNoun}`}
      severity={count > 0 ? "info" : "warning"}
    />
  );
}
