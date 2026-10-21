import type { ReactNode } from "react";
import { useFormContext, useWatch, type FieldValues } from "react-hook-form";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Tag } from "primereact/tag";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import { FormInputTextAreaComponent } from "@/shared/components/form/FormInputTextAreaComponent";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import {
  flowStepAccent,
  getFlowStepCatalogEntry,
  tint,
} from "@/shared/models/flow-step-catalog";
import { describeFlowStep } from "@/features/flow-step/components/forms/shared/flow-step-description";
import { useFlowStepScriptLine } from "@/features/flow-step/hooks/use-flow-step-script-line";

interface Props {
  formMode: FormMode;
  isValid: boolean;
  isDirty: boolean;
  onEdit: () => void;
  onCancel: () => void;

  /** The step the form was opened with, which the values on the form are laid over. */
  baseValues: FlowStepDto;

  /** How the form turns its values into a step, when that is more than laying them over. */
  toDto?: (values: FieldValues) => FlowStepDto;

  /** Instead of the ones read from the values, for a form that knows better. */
  mode?: string;
  summary?: string;

  /** Left of Cancel and Save, in every mode. */
  testAction?: ReactNode;

  children?: ReactNode;
}

/**
 * Every step's form, around its own fields: the step's type, name and mode, a sentence of what it
 * will do, what it gives later steps, the Comment, the line it will be in the script, and Save.
 * Inside the form's FormProvider, since it reads and binds the values.
 */
export function FlowStepFormLayoutComponent({
  formMode,
  isValid,
  isDirty,
  onEdit,
  onCancel,
  baseValues,
  toDto,
  mode,
  summary,
  testAction,
  children,
}: Props) {
  const { control } = useFormContext();
  const values = useWatch({ control });

  const dto = toDto ? toDto(values) : new FlowStepDto({ ...baseValues, ...(values as Partial<FlowStepDto>) });
  const { data: line, isError } = useFlowStepScriptLine(dto);
  const description = describeFlowStep(dto, line);

  const accent = flowStepAccent(dto.flowStepType);
  const entry = getFlowStepCatalogEntry(dto.flowStepType);
  const shownMode = mode ?? description.mode;
  const shownSummary = summary ?? description.summary;
  const isEditing = formMode === FormMode.ADD || formMode === FormMode.EDIT;

  return (
    <div className="flex flex-column">
      <div className="flex align-items-center gap-3 pb-3 mb-3 border-bottom-1 surface-border">
        <span
          className="flex align-items-center justify-content-center flex-shrink-0 w-3rem h-3rem border-round-lg"
          style={{ color: accent, background: tint(accent, 18) }}
        >
          <IconComponent name={entry?.iconName ?? "circle"} />
        </span>

        <div className="flex flex-column flex-1 min-w-0">
          <span className="text-sm text-color-secondary">{description.type}</span>
          <span className="text-xl font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {line?.name || dto.name || description.type}
          </span>
        </div>

        {shownMode && (
          <Tag
            value={shownMode}
            className="flex-shrink-0 font-normal"
            style={{ color: accent, background: tint(accent, 15) }}
          />
        )}

        {formMode === FormMode.VIEW && (
          <Button
            type="button"
            icon="pi pi-pencil"
            label="Edit"
            outlined
            severity="secondary"
            onClick={onEdit}
          />
        )}
      </div>

      {shownSummary && (
        <Message
          severity="info"
          className="w-full justify-content-start mb-2"
          text={shownSummary}
        />
      )}

      {children}

      {description.gives && (
        <div className="flex gap-2 align-items-baseline text-sm mt-2">
          <span
            className="text-xs font-semibold text-color-secondary flex-shrink-0"
            style={{ letterSpacing: "0.06em" }}
          >
            GIVES
          </span>
          <span className="text-color-secondary">{description.gives}</span>
        </div>
      )}

      <div className="mt-4 pt-3 border-top-1 surface-border">
        <FormInputTextAreaComponent
          fieldName="codeComment"
          label="Comment"
          placeholderText="What you expect here, and what it means if it fails."
          hintText="Printed above the step in its script as a # comment, read by whoever reviews it, and quoted back when a failure is explained."
          rows={2}
          maxLength={1000}
          isDisabled={!isEditing}
        />
      </div>

      <div className="flex flex-column gap-2">
        <LabelComponent
          text="In the script"
          weight="bold"
        />
        {isError ? (
          <LabelComponent
            text="The line appears once the step has what it needs."
            size="sm"
            color="secondary"
          />
        ) : (
          <ScriptViewComponent
            script={line?.script ?? ""}
            maxHeight={160}
          />
        )}
      </div>

      <div className="flex flex-wrap align-items-center gap-3 mt-5 pt-3 border-top-1 surface-border">
        {testAction && <div className="flex-shrink-0">{testAction}</div>}
        <div className="flex-1" />
        {isEditing && (
          <>
            <Button
              type="button"
              label="Cancel"
              severity="secondary"
              text
              onClick={onCancel}
            />
            <Button
              type="submit"
              label="Save"
              icon="pi pi-check"
              disabled={!isValid || (formMode === FormMode.EDIT && !isDirty)}
            />
          </>
        )}
      </div>
    </div>
  );
}
