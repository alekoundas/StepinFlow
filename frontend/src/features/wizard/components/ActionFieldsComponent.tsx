import { useEffect, useRef } from "react";
import { FormProvider, useForm } from "react-hook-form";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import FlowStepSearchAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepSearchAreaFieldComponent";
import type { ActionAnswers } from "@/features/wizard/action-to-steps";

interface Props {
  optionId: string;
  answers: ActionAnswers;
  flowId: number | undefined;

  /** Cropping needs a screenshot, which not every action has. */
  canCrop: boolean;
  onCrop: () => void;

  onChange: (answers: ActionAnswers) => void;
}

/**
 * Only the fields the recording could not already know.
 *
 * Deliberately not the step's whole form. Most of what a recorded step needs is already right,
 * and a full form embedded mid-assembly asks about things the wizard fills in at save time and
 * then reports them as errors. Anything deeper than this is a job for the tree editor, where
 * editing a saved step already works properly.
 */
export default function ActionFieldsComponent({
  optionId,
  answers,
  flowId,
  canCrop,
  onCrop,
  onChange,
}: Props) {
  const form = useForm<Record<string, unknown>>({
    mode: "onChange",
    defaultValues: { ...answers } as never,
  });

  // Held in refs so the subscription below is set up once rather than torn down on every
  // keystroke, which would drop the field being typed in. Reading `answers` through a ref rather
  // than the closure matters: the effect runs once, so the captured value is whatever the answers
  // were on the first render, and merging into that would replay stale ones on every change.
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;

  const answersRef = useRef(answers);
  answersRef.current = answers;

  useEffect(() => {
    const subscription = form.watch((next) =>
      onChangeRef.current({
        ...answersRef.current,
        ...next,
        // The crop is not a form field, so nothing the form reports can speak for it. Picking a
        // search area used to hand back a `template` of undefined and clear the crop.
        template: answersRef.current.template,
      } as ActionAnswers),
    );

    return () => subscription.unsubscribe();
  }, [form]);

  const hasTemplate = answers.template != null;

  const isSearch =
    optionId === "image-click" ||
    optionId === "image-only" ||
    optionId === "wait-for-image";

  return (
    <FormProvider {...form}>
      <div className="flex flex-column gap-2">
        <FormInputTextComponent
          fieldName="name"
          label="Name"
          hintText="What this step is called in the tree."
        />

        {isSearch && (
          <>
            <div className="flex align-items-center gap-3">
              {/* The crop is what the search actually matches on, so it is worth seeing. */}
              {hasTemplate ? (
                <img
                  src={`data:image/png;base64,${answers.template}`}
                  alt=""
                  className="border-round"
                  style={{
                    maxWidth: "10rem",
                    maxHeight: "6rem",
                    objectFit: "contain",
                    border: "1px solid var(--surface-border)",
                  }}
                />
              ) : (
                <LabelComponent
                  text="No template yet, so there is nothing to look for."
                  size="sm"
                  color="error"
                />
              )}

              <Button
                type="button"
                label={hasTemplate ? "Re-crop" : "Crop the template"}
                icon="pi pi-crop"
                onClick={onCrop}
                disabled={!canCrop}
                className="p-button-sm p-button-outlined"
              />
            </div>

            <FlowStepSearchAreaFieldComponent
              flowId={flowId}
              labelText="Where to look"
              hintText="A smaller area is the single biggest thing that makes the search fast."
            />
          </>
        )}

        {optionId === "scroll" && (
          <div className="flex gap-3">
            <FormSelectButtonComponent
              fieldName="cursorScrollDirectionType"
              labelText="Direction"
              options={[
                { label: "Up", value: CursorScrollDirectionTypeEnum.UP },
                { label: "Down", value: CursorScrollDirectionTypeEnum.DOWN },
              ]}
              classNameContainer="flex-1"
            />
            <FormInputNumberComponent
              fieldName="loopCount"
              label="Notches"
              min={1}
              max={2147483647}
              className="flex-1"
            />
          </div>
        )}

        {optionId === "type-text" && (
          <FormInputTextComponent
            fieldName="keyboardInputText"
            label="Text to type"
            hintText="The recorder cannot tell capitals from lower case, so check it reads right."
          />
        )}

        {optionId === "send-keys" && (
          <FormInputTextComponent
            fieldName="keyboardInputText"
            label="Keys to send"
          />
        )}

        {optionId === "wait" && (
          <FormInputNumberComponent
            fieldName="waitForMilliseconds"
            label="Wait for (ms)"
            min={0}
            max={2147483647}
            hintText="Measured from how long you actually paused."
          />
        )}

        {optionId === "wait-for-image" && (
          <FormInputNumberComponent
            fieldName="timeoutMilliseconds"
            label="Give up after (ms)"
            min={0}
            max={2147483647}
            hintText="0 = wait forever."
          />
        )}
      </div>
    </FormProvider>
  );
}
