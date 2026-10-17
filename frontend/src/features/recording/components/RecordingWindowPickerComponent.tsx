import { useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { RadioButton } from "primereact/radiobutton";
import { SelectButton } from "primereact/selectbutton";

import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import type { WindowMatchDto } from "@/shared/models/window-match-dto";
import type { RecordingWindowForm } from "@/features/recording/components/recording-setup.zod";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };
const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

const FIND_BY_OPTIONS = [
  { label: "Running app", value: "APP" },
  { label: "Window title", value: "TITLE" },
];

const MATCH_MODE_OPTIONS = [
  { label: "contains", value: TitleMatchModeEnum.CONTAINS },
  { label: "equals", value: TitleMatchModeEnum.EQUALS },
  { label: "starts with", value: TitleMatchModeEnum.STARTS_WITH },
  { label: "matches regex", value: TitleMatchModeEnum.REGEX },
];

interface OpenWindow {
  key: string;
  title: string;
  processName: string;
}

interface Props {
  // Fields of the same section that go between the title and the test.
  children?: ReactNode;
}

/**
 * The window a recording works in, which becomes the flow's main area: picked from the windows
 * open now, or matched on a title for an app that is not running yet. The same matching the area
 * form and the window step use, laid out as the recording setup is.
 */
export default function RecordingWindowPickerComponent({ children }: Props) {
  const { control, setValue, getValues } = useFormContext<RecordingWindowForm>();
  const titleMatchMode = useWatch({ control, name: "titleMatchMode" });

  const [findBy, setFindBy] = useState("APP");
  const [pickedKey, setPickedKey] = useState<string | null>(null);
  const [isTesting, setIsTesting] = useState(false);
  const [matches, setMatches] = useState<WindowMatchDto[] | null>(null);
  const [testError, setTestError] = useState<string | null>(null);

  // Live windows are stale the moment they are listed, so nothing is cached.
  const { data: openWindows = [], refetch, isFetching } = useQuery({
    queryKey: ["lookup", "window", "recording-setup"],
    queryFn: loadOpenWindows,
    staleTime: 0,
    enabled: findBy === "APP",
  });

  const clearTest = () => {
    setMatches(null);
    setTestError(null);
  };

  const pick = (openWindow: OpenWindow) => {
    setPickedKey(openWindow.key);
    setValue("processName", openWindow.processName, { shouldValidate: true, shouldDirty: true });
    clearTest();
  };

  // A process name left behind would keep narrowing the search to an app no longer named.
  const switchFindBy = (next: string | null) => {
    if (!next) return;

    setFindBy(next);
    clearTest();

    if (next === "TITLE") {
      setPickedKey(null);
      setValue("processName", "", { shouldValidate: true, shouldDirty: true });
    }
  };

  const test = async () => {
    setIsTesting(true);
    clearTest();

    try {
      const result = await backendApiService.Lookup.testWindowMatch({
        processName: getValues("processName"),
        titlePattern: getValues("titlePattern"),
        titleMatchMode: getValues("titleMatchMode"),
        useClientArea: getValues("useClientArea"),
        bringsToFront: true,
      });

      setMatches(result.matches);
    } catch (err) {
      setTestError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsTesting(false);
    }
  };

  const matchLabel = MATCH_MODE_OPTIONS.find((x) => x.value === titleMatchMode)?.label ?? "contains";

  return (
    <div className="flex flex-column gap-4">
      <SelectButton
        value={findBy}
        options={FIND_BY_OPTIONS}
        optionLabel="label"
        optionValue="value"
        onChange={(e) => switchFindBy(e.value)}
        allowEmpty={false}
        className="align-self-start"
      />

      {findBy === "APP" && (
        <div className="flex flex-column gap-2">
          <div className="flex align-items-center justify-content-between">
            <span
              id="recording-window-label"
              className="font-medium"
            >
              Pick the window
            </span>
            <Button
              type="button"
              icon="pi pi-refresh"
              aria-label="Reload the list of open windows"
              onClick={() => void refetch()}
              loading={isFetching}
              size="small"
              text
            />
          </div>

          <div
            role="radiogroup"
            aria-labelledby="recording-window-label"
            className="flex flex-column gap-2 overflow-auto"
            style={{ maxHeight: 280 }}
          >
            {openWindows.map((openWindow) => (
              <OpenWindowComponent
                key={openWindow.key}
                openWindow={openWindow}
                isPicked={openWindow.key === pickedKey}
                onPick={() => pick(openWindow)}
              />
            ))}

            {openWindows.length === 0 && !isFetching && (
              <span
                className="text-sm"
                style={MUTED}
              >
                No windows are open. Open the app, then reload the list.
              </span>
            )}
          </div>

          <span
            className="text-xs"
            style={MUTED}
          >
            Windows that are open now, the one in front first. StepinFlow's own are left out.
          </span>
        </div>
      )}

      <div className="flex flex-wrap gap-3">
        {findBy === "APP" && (
          <div style={{ flex: "1 1 180px" }}>
            <FormInputTextComponent
              fieldName="processName"
              label="Process"
              hintText="Matched on the process name, which does not change while the app runs."
            />
          </div>
        )}
        <div style={{ flex: "0 0 160px" }}>
          <FormDropdownComponent<Record<string, unknown>, { label: string; value: string }>
            fieldName="titleMatchMode"
            labelText="Match"
            mode="local"
            options={MATCH_MODE_OPTIONS}
            optionLabel="label"
            optionValue="value"
          />
        </div>
        <div style={{ flex: "2 1 240px" }}>
          <FormInputTextComponent
            fieldName="titlePattern"
            label={`Title ${matchLabel}`}
            isRequired={findBy === "TITLE"}
            hintText={
              findBy === "TITLE"
                ? "The title identifies the window, so this can name an app that is not running yet."
                : "Optional. Tells two windows of the same app apart."
            }
          />
        </div>
      </div>

      {children}

      <div
        className="flex align-items-center flex-wrap gap-3 px-3 py-2 border-1 border-round-lg"
        style={{ background: "var(--surface-ground)", borderColor: "var(--surface-border)" }}
      >
        <Button
          type="button"
          label="Test"
          icon="pi pi-play"
          onClick={() => void test()}
          loading={isTesting}
          size="small"
          outlined
        />
        <TestStatusComponent
          matches={matches}
          error={testError}
        />
      </div>
    </div>
  );
}

function OpenWindowComponent({
  openWindow,
  isPicked,
  onPick,
}: {
  openWindow: OpenWindow;
  isPicked: boolean;
  onPick: () => void;
}) {
  const inputId = `recording-window-${openWindow.key}`;

  let frame: CSSProperties = { borderColor: "var(--surface-border)", background: "var(--surface-ground)" };
  if (isPicked)
    frame = {
      borderColor: "var(--primary-color)",
      background: "color-mix(in srgb, var(--primary-color) 10%, transparent)",
    };

  return (
    <label
      htmlFor={inputId}
      className="flex align-items-center gap-3 px-3 py-2 border-1 border-round-lg cursor-pointer"
      style={frame}
    >
      <RadioButton
        inputId={inputId}
        name="recording-window"
        checked={isPicked}
        onChange={onPick}
      />
      <span
        aria-hidden="true"
        className="flex align-items-center justify-content-center border-round-lg font-semibold text-sm flex-none"
        style={{ width: 32, height: 32, background: "var(--surface-hover)", ...MUTED }}
      >
        {openWindow.processName.charAt(0).toUpperCase()}
      </span>
      <span className="flex flex-column flex-1 min-w-0">
        <span className="font-medium white-space-nowrap overflow-hidden text-overflow-ellipsis">
          {openWindow.title}
        </span>
        <span
          className="text-xs"
          style={{ ...MONO, ...MUTED }}
        >
          {openWindow.processName}
        </span>
      </span>
    </label>
  );
}

// The count leads: the first match is the one the flow takes, so two means it takes whichever is in front.
function TestStatusComponent({ matches, error }: { matches: WindowMatchDto[] | null; error: string | null }) {
  if (error)
    return (
      <span
        className="text-sm"
        style={{ color: "var(--red-400)" }}
      >
        {error}
      </span>
    );

  if (matches === null)
    return (
      <span
        className="text-sm"
        style={MUTED}
      >
        Finds the window and brings it to the front.
      </span>
    );

  if (matches.length === 0)
    return (
      <StatusLineComponent dot="var(--yellow-500)">
        Nothing matches. Check the app is open, or loosen the title.
      </StatusLineComponent>
    );

  const first = matches[0];
  if (matches.length === 1)
    return (
      <StatusLineComponent dot="var(--green-400)">
        Found 1 window and brought it to the front:{" "}
        <span style={{ ...MONO, ...MUTED }}>
          {first.width}×{first.height}
        </span>
      </StatusLineComponent>
    );

  return (
    <StatusLineComponent dot="var(--yellow-500)">
      {matches.length} windows match. The flow takes the one in front: {first.title}
    </StatusLineComponent>
  );
}

function StatusLineComponent({ dot, children }: { dot: string; children: ReactNode }) {
  return (
    <span
      role="status"
      className="flex align-items-center gap-2 text-sm"
    >
      <span
        className="border-circle flex-none"
        style={{ width: 8, height: 8, background: dot }}
      />
      <span>{children}</span>
    </span>
  );
}

async function loadOpenWindows(): Promise<OpenWindow[]> {
  const electronProcessId = await window.electronApi.electronProcess.getId();
  const response = await backendApiService.Lookup.window({ searchText: "" });

  return response.data
    .filter((item) => item.extraData?.processId !== electronProcessId)
    .map((item, index) => ({
      key: `${index}-${String(item.value)}`,
      title: item.label,
      processName: String(item.value),
    }));
}
