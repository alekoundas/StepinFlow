import { useState } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { Button } from "primereact/button";
import { useQueryClient } from "@tanstack/react-query";

import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

/** Lowercase because they read as the start of a sentence the text field finishes. */
const MATCH_MODE_OPTIONS = [
  { label: "contains", value: TitleMatchModeEnum.CONTAINS },
  { label: "equals", value: TitleMatchModeEnum.EQUALS },
  { label: "starts with", value: TitleMatchModeEnum.STARTS_WITH },
  { label: "matches regex", value: TitleMatchModeEnum.REGEX },
];

interface WindowOption {
  label: string;
  value: string;
  description?: string;
}

interface Props {
  isDisabled?: boolean;

  /**
   * Fires when an application is picked, with its label. The caller decides what to do with it —
   * this component has no idea what the record it lives in is called.
   */
  onWindowPicked?: (label: string) => void;
}

/**
 * Says which window something means. Shared by the window step and the area form, so the two can
 * never drift apart.
 *
 * Two ways in, because they answer different situations. Picking a running app is the common case
 * and matches on the process name, which does not change while the app runs. Typing a title is the
 * escape hatch: the dropdown only lists what is open, so it cannot name an app that is closed while
 * the flow is being built.
 *
 * Both are ANDed when both are set, which is the only way to tell two windows of the same app
 * apart. That is why the title stays available as narrowing rather than disappearing in app mode.
 *
 * Which branch a saved row opens in is derived, not stored: a process name means the app branch.
 */
export default function WindowMatchFieldsComponent({
  isDisabled = false,
  onWindowPicked,
}: Props) {
  const { control, setValue } = useFormContext();
  const queryClient = useQueryClient();

  const processName = useWatch({ control, name: "processName" }) as string | undefined;

  const [isByTitle, setIsByTitle] = useState(!processName);

  const loadWindows = (filter?: string): Promise<WindowOption[]> =>
    backendApiService.Lookup.window({ searchText: filter }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: String(item.value),
        description: item.description,
      })),
    );

  // The list is of live windows, so it is stale the moment it is fetched. The query itself is set
  // not to cache; this is for when something opened while the form was already up.
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["lookup", "window"] });

  const switchMode = (byTitle: boolean) => {
    setIsByTitle(byTitle);

    // Leaving a process name behind while the user matches on a title would keep narrowing the
    // search to an app they stopped naming.
    if (byTitle) setValue("processName", "", { shouldValidate: true, shouldDirty: true });
  };

  return (
    <div className="flex flex-column gap-2">
      <LabelComponent
        text="Find the window by"
        size="sm"
      />

      <div className="flex mb-2">
        <Button
          type="button"
          label="Running app"
          disabled={isDisabled}
          onClick={() => switchMode(false)}
          className={`flex-1 border-noround-right ${isByTitle ? "p-button-outlined" : ""}`}
        />
        <Button
          type="button"
          label="Window title"
          disabled={isDisabled}
          onClick={() => switchMode(true)}
          className={`flex-1 border-noround-left ${isByTitle ? "" : "p-button-outlined"}`}
        />
      </div>

      {!isByTitle && (
        <div className="flex gap-2 align-items-end">
          <div className="flex-1">
            <FormDropdownComponent<Record<string, unknown>, WindowOption>
              fieldName="processName"
              labelText="Application"
              mode="remote"
              queryKey={["lookup", "window"]}
              queryFn={loadWindows}
              optionLabel="label"
              optionValue="value"
              placeholderText="Search open windows..."
              isDisabled={isDisabled}
              defaultValue={""}
              hintText="Matched on the process name, which does not change while the app runs."
              staleTime={0}
              itemTemplate={(item) => (
                <div className="flex flex-column">
                  <LabelComponent text={item.label} />
                  {item.description && (
                    <LabelComponent
                      text={item.description}
                      size="xs"
                      color="secondary"
                    />
                  )}
                </div>
              )}
              onChanged={(_value, option) => {
                if (option) onWindowPicked?.(option.label);
              }}
            />
          </div>

          <Button
            type="button"
            icon="pi pi-refresh"
            aria-label="Reload the list of open windows"
            onClick={() => void refresh()}
            disabled={isDisabled}
            className="p-button-outlined mb-3"
            tooltip="Reload the list of open windows"
            tooltipOptions={{ position: "top" }}
          />
        </div>
      )}

      {!isByTitle && (
        <LabelComponent
          text="Narrow it down — optional"
          size="xs"
          color="secondary"
        />
      )}

      <div className="flex gap-3">
        <div className="flex-1">
          <FormDropdownComponent<Record<string, unknown>, { label: string; value: string }>
            fieldName="titleMatchMode"
            labelText="Title match"
            mode="local"
            options={MATCH_MODE_OPTIONS}
            optionLabel="label"
            optionValue="value"
            isDisabled={isDisabled}
          />
        </div>
        <div className="flex-1">
          <FormInputTextComponent
            fieldName="titlePattern"
            label="Window title"
            isRequired={isByTitle}
            isDisabled={isDisabled}
            hintText={
              isByTitle
                ? "The title identifies the window, so this can name an app that is not running yet."
                : undefined
            }
          />
        </div>
      </div>
    </div>
  );
}
