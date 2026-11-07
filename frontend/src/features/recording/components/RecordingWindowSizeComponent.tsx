import { useState } from "react";
import type { CSSProperties } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { Button } from "primereact/button";

import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import type { WindowMatchDto } from "@/shared/models/window-match-dto";
import type { RecordingSetupForm } from "@/features/recording/components/recording-setup.zod";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };
const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

interface Measured {
  processName: string;
  windows: WindowMatchDto[];
}

/**
 * The size the flow sets its window to, as a step after the launch: typed, or copied from one of
 * the app's open windows. Record now sets it there and then, so the recording is at that size.
 *
 * The whole window, title bar and borders included, because that is what Resize Window sets: the
 * inside measured here would shrink the window a little on every execution.
 */
export default function RecordingWindowSizeComponent() {
  const { control, setValue, getValues } = useFormContext<RecordingSetupForm>();
  const addsResizeStep = useWatch({ control, name: "addsResizeStep" });
  const processName = useWatch({ control, name: "processName" });
  const titlePattern = useWatch({ control, name: "titlePattern" });
  const areaType = useWatch({ control, name: "areaType" });

  // Kept with the app it was measured for, so picking another one hides a list that is not its.
  const [measured, setMeasured] = useState<Measured | null>(null);
  const [isMeasuring, setIsMeasuring] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isWebsite = areaType === FlowAreaTypeEnum.BROWSER_TAB;
  const canMeasure = processName.length > 0 || (!isWebsite && titlePattern.length > 0);

  // A browser's title is its front tab's, so its windows are found by the browser alone.
  const measure = async () => {
    setIsMeasuring(true);
    setError(null);

    try {
      const result = await backendApiService.Lookup.testWindowMatch({
        processName,
        titlePattern: isWebsite ? "" : titlePattern,
        titleMatchMode: getValues("titleMatchMode"),
        ignoreWindowBorders: false,
      });

      setMeasured({ processName, windows: result.matches });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsMeasuring(false);
    }
  };

  const copySize = (match: WindowMatchDto) => {
    setValue("windowWidth", match.width, { shouldValidate: true, shouldDirty: true });
    setValue("windowHeight", match.height, { shouldValidate: true, shouldDirty: true });
  };

  let hint = "Lists its open windows with their sizes, to copy one.";
  if (!canMeasure) hint = isWebsite ? "Pick the browser first." : "Pick the application first.";

  const shown = measured !== null && measured.processName === processName ? measured.windows : null;

  return (
    <div className="flex flex-column gap-3">
      <FormInputCheckboxComponent
        fieldName="addsResizeStep"
        label="Also add a step that resizes the window"
        hintText="Every execution sets the window to this size, and Record now sets it too, so what you record is at that size."
      />

      {addsResizeStep && (
        <div className="flex flex-column gap-3 pl-4">
          <div className="flex flex-wrap gap-3">
            <div style={{ flex: "1 1 140px" }}>
              <FormInputNumberComponent
                fieldName="windowWidth"
                label="Width"
                min={0}
                isRequired={true}
              />
            </div>
            <div style={{ flex: "1 1 140px" }}>
              <FormInputNumberComponent
                fieldName="windowHeight"
                label="Height"
                min={0}
                isRequired={true}
                hintText="Title bar and borders included."
              />
            </div>
          </div>

          <div className="flex align-items-center flex-wrap gap-3">
            <Button
              type="button"
              label="Measure its windows"
              icon="pi pi-arrows-alt"
              onClick={() => void measure()}
              loading={isMeasuring}
              disabled={!canMeasure || isMeasuring}
              size="small"
              outlined
            />
            <span
              className="text-sm"
              style={error ? { color: "var(--red-400)" } : MUTED}
            >
              {error ?? hint}
            </span>
          </div>

          {shown !== null && shown.length === 0 && (
            <span
              className="text-sm"
              style={MUTED}
            >
              None of its windows are open. Open it, then measure again.
            </span>
          )}

          {shown !== null && shown.length > 0 && (
            <div
              className="flex flex-column gap-2 overflow-auto"
              style={{ maxHeight: 220 }}
            >
              {shown.map((match, index) => (
                <div
                  key={`${index}-${match.title}`}
                  className="flex align-items-center gap-3 px-3 py-2 border-1 border-round-lg"
                  style={{ borderColor: "var(--surface-border)", background: "var(--surface-ground)" }}
                >
                  <span className="flex flex-column flex-1 min-w-0">
                    <span className="font-medium white-space-nowrap overflow-hidden text-overflow-ellipsis">
                      {match.title}
                    </span>
                    <span
                      className="text-xs"
                      style={{ ...MONO, ...MUTED }}
                    >
                      {match.width} × {match.height}
                    </span>
                  </span>
                  <Button
                    type="button"
                    label="Use this size"
                    onClick={() => copySize(match)}
                    size="small"
                    text
                  />
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
