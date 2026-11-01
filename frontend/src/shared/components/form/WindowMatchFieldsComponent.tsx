import { useState } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { useQueryClient } from "@tanstack/react-query";
import { classNames } from "primereact/utils";

import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import type { WindowMatchDto } from "@/shared/models/window-match-dto";

/** Lowercase because they read as the start of a sentence the text field finishes. */
const MATCH_MODE_OPTIONS = [
  { label: "contains", value: TitleMatchModeEnum.CONTAINS },
  { label: "equals", value: TitleMatchModeEnum.EQUALS },
  { label: "starts with", value: TitleMatchModeEnum.STARTS_WITH },
  { label: "matches regex", value: TitleMatchModeEnum.REGEX },
];

const SHOWN_TITLES = 3;

interface AppOption {
  label: string;
  value: string;
  windowTitles: string[];

  /** One of its windows', which tells the backend where its program is. */
  processId: number;
}

interface Props {
  isDisabled?: boolean;

  /** Mirrors the area's own checkbox so the tested bounds are the ones it will really resolve to. */
  useClientArea?: boolean;

  /**
   * Fires when an application is picked, with its name and whether it replaced another. The caller
   * decides what to do with it — this component has no idea what the record it lives in is called.
   */
  onAppPicked?: (app: string, processId: number, isAnotherApp: boolean) => void;

  /** Testing also brings the window it found to the front, so the tester sees which one it is. */
  bringsToFront?: boolean;

  /**
   * A browser tab's: only which browser. Its title is its front tab's, so a title would match only
   * while that tab was in front, and a test of the window would say nothing about the tab.
   */
  isBrowser?: boolean;
}

/**
 * Says which window an area means. A window step names that area rather than describing a window of
 * its own, so this is the one place a window is described.
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
  useClientArea = false,
  onAppPicked,
  bringsToFront = false,
  isBrowser = false,
}: Props) {
  const { control, setValue, getValues } = useFormContext();
  const queryClient = useQueryClient();

  const processName = useWatch({ control, name: "processName" }) as string | undefined;

  const [isByTitle, setIsByTitle] = useState(!processName && !isBrowser);

  const [isTesting, setIsTesting] = useState(false);
  const [matches, setMatches] = useState<WindowMatchDto[] | null>(null);
  const [testError, setTestError] = useState<string | null>(null);

  // A row an app, front first, and never StepinFlow's own windows, which would otherwise top the
  // list. The area matches on the process, so a window's title would promise an instance it never
  // picks; the titles go under the app, to say which one it is.
  const loadApps = async (filter?: string): Promise<AppOption[]> => {
    const electronProcessId = await window.electronApi.electronProcess.getId();
    const res = await backendApiService.Lookup.window({ searchText: filter });

    const apps = new Map<string, AppOption>();
    for (const item of res.data) {
      const processId = item.extraData?.processId as number | undefined;
      if (processId === undefined || processId === electronProcessId) continue;

      const app = String(item.value);
      const option = apps.get(app) ?? { label: app, value: app, windowTitles: [], processId };
      option.windowTitles.push(item.label);
      apps.set(app, option);
    }

    return [...apps.values()];
  };

  // The list is of live windows, so it is stale the moment it is fetched. The query itself is set
  // not to cache; this is for when something opened while the form was already up.
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["lookup", "window"] });

  /**
   * The matcher is the one part of a flow with no feedback until it runs, and the count is the
   * interesting half: the first match is the one that runs, so two matches means the flow takes
   * whichever window happens to be in front.
   */
  const test = async () => {
    setIsTesting(true);
    setTestError(null);
    setMatches(null);

    try {
      const result = await backendApiService.Lookup.testWindowMatch({
        processName: (getValues("processName") as string) ?? "",
        titlePattern: (getValues("titlePattern") as string) ?? "",
        titleMatchMode: getValues("titleMatchMode") as TitleMatchModeEnum,
        useClientArea,
        bringsToFront,
      });

      setMatches(result.matches);
    } catch (err) {
      setTestError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsTesting(false);
    }
  };

  const switchMode = (byTitle: boolean) => {
    setIsByTitle(byTitle);
    setMatches(null);
    setTestError(null);

    // Leaving a process name behind while the user matches on a title would keep narrowing the
    // search to an app they stopped naming.
    if (byTitle) setValue("processName", "", { shouldValidate: true, shouldDirty: true });
  };

  return (
    <div className="flex flex-column gap-2">
      {!isBrowser && (
        <LabelComponent
          text="Find the window by"
          size="sm"
        />
      )}

      <div className={classNames("mb-2", isBrowser ? "hidden" : "flex")}>
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
            <FormDropdownComponent<Record<string, unknown>, AppOption>
              fieldName="processName"
              labelText={isBrowser ? "Browser" : "Application"}
              mode="remote"
              queryKey={["lookup", "window"]}
              queryFn={loadApps}
              optionLabel="label"
              optionValue="value"
              placeholderText={isBrowser ? "Search open browsers..." : "Search open apps..."}
              isDisabled={isDisabled}
              defaultValue={""}
              hintText={
                isBrowser
                  ? "Every window of it is looked in for the tab."
                  : "Matched on the process name, which does not change while the app runs."
              }
              staleTime={0}
              itemTemplate={(item) => (
                <div className="flex flex-column">
                  <LabelComponent text={item.label} />
                  {item.windowTitles.slice(0, SHOWN_TITLES).map((title, index) => (
                    <LabelComponent
                      key={index}
                      text={title}
                      size="xs"
                      color="secondary"
                    />
                  ))}
                  {item.windowTitles.length > SHOWN_TITLES && (
                    <LabelComponent
                      text={`and ${item.windowTitles.length - SHOWN_TITLES} more`}
                      size="xs"
                      color="secondary"
                    />
                  )}
                </div>
              )}
              // The saved app, which is not in the list while it is closed.
              valueTemplate={(_option, props) => (props.value as string) || props.placeholder}
              onChanged={(_value, option) => {
                if (option) onAppPicked?.(option.value, option.processId, option.value !== processName);
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

      {!isByTitle && !isBrowser && (
        <LabelComponent
          text="Narrow it down — optional"
          size="xs"
          color="secondary"
        />
      )}

      <div className={classNames("gap-3", isBrowser ? "hidden" : "flex")}>
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

      {!isDisabled && !isBrowser && (
        <div className="flex flex-column gap-2 mt-2">
          <div>
            <Button
              type="button"
              label={isTesting ? "Looking..." : "Test the match"}
              icon="pi pi-search"
              loading={isTesting}
              disabled={isTesting}
              onClick={() => void test()}
              className="p-button-outlined p-button-sm"
            />
          </div>

          {testError && (
            <Message
              severity="error"
              className="w-full justify-content-start"
              text={testError}
            />
          )}

          {matches !== null && <MatchResult matches={matches} />}
        </div>
      )}
    </div>
  );
}

/**
 * What the matcher found, right now, on this machine.
 *
 * Deliberately leads with the count rather than a verdict. One match is the answer; several means
 * the flow will take whichever is frontmost, which is the thing a matcher gets wrong silently.
 */
function MatchResult({ matches }: { matches: WindowMatchDto[] }) {
  if (matches.length === 0)
    return (
      <Message
        severity="warn"
        className="w-full justify-content-start"
        text="Nothing matches. Check the app is open, or loosen the title."
      />
    );

  const headline =
    matches.length === 1
      ? "1 window matches"
      : `${matches.length} windows match — using the frontmost`;

  return (
    <div className="surface-100 border-round p-3 flex flex-column gap-2">
      <LabelComponent
        text={headline}
        size="sm"
        weight="semibold"
        color={matches.length === 1 ? "success" : "warning"}
      />

      {matches.map((match, index) => (
        <div
          key={`${match.processName}-${index}`}
          className="flex flex-column"
        >
          <LabelComponent
            text={`${index === 0 ? "▸ " : "   "}${match.title}`}
            size="sm"
            color={index === 0 ? "primary" : "secondary"}
          />
          <LabelComponent
            text={`${match.processName} · ${match.width} × ${match.height} at ${match.x}, ${match.y}`}
            size="xs"
            color="secondary"
          />
        </div>
      ))}

      <LabelComponent
        text="Tested against this machine. It catches typos and patterns that are too broad, not whether the app exists wherever the flow finally runs."
        size="xs"
        color="secondary"
      />
    </div>
  );
}
