import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Dropdown } from "primereact/dropdown";
import { classNames } from "primereact/utils";

import { backendApiService } from "@/shared/services/backend-api-service";

interface BotOption {
  label: string;
  value: number;
}

interface Props {
  inputId: string;
  value: number | null;
  onChange: (discordBotId: number | null) => void;

  /** Add a bot and use it: in a dialog, or in the window's own next screen. */
  onNew: () => void;
}

/** The bots a notification can send through, and New beside them. Without one it sends nothing. */
export default function DiscordBotPickerComponent({ inputId, value, onChange, onNew }: Props) {
  const { data: bots = [] } = useQuery({
    queryKey: ["lookup", "discordBot", "gesture"],
    queryFn: () =>
      backendApiService.Lookup.discordBot({ searchText: "" }).then((res) =>
        res.data.map((item): BotOption => ({ label: item.label, value: Number(item.value) })),
      ),
  });

  return (
    <div className="flex align-items-center gap-2 w-full">
      <Dropdown
        inputId={inputId}
        value={value}
        options={bots}
        optionLabel="label"
        optionValue="value"
        placeholder="Choose a bot..."
        emptyMessage="No bots yet. Add one with New."
        onChange={(e) => onChange(typeof e.value === "number" ? e.value : null)}
        className={classNames("flex-1 min-w-0", value === null && "p-invalid")}
      />
      <Button
        type="button"
        icon="pi pi-plus"
        label="New"
        onClick={onNew}
        outlined
      />
    </div>
  );
}
