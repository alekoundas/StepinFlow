import { useFormContext, useWatch } from "react-hook-form";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KEYBOARD_MODES } from "@/features/flow-step/components/forms/keyboard/keyboard-modes";
import {
  toDisplay,
  useCaptureCombination,
} from "@/features/flow-step/hooks/use-capture-combination";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepKeyboardFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();
  const mode = useWatch({ control, name: "keyboardInputType" });
  const combination = useWatch({ control, name: "keyboardInputText" }) as string;

  const { captureCombination, cancelCapture, isCapturing, heldKeys } =
    useCaptureCombination();

  const isCombination = mode === KeyboardInputTypeEnum.COMBINATION;

  const record = async () => {
    const captured = await captureCombination();
    if (captured === null) return;

    setValue("keyboardInputText", captured, {
      shouldDirty: true,
      shouldValidate: true,
    });
  };

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <FormSelectButtonComponent
        fieldName="keyboardInputType"
        labelText="Mode"
        options={KEYBOARD_MODES.map((x) => ({ label: x.label, value: x.value }))}
        isRequired={true}
        isDisabled={isDisabled || isCapturing}
        hintText={KEYBOARD_MODES.find((x) => x.value === mode)?.description}
      />

      {!isCombination && (
        <FormInputTextComponent
          fieldName="keyboardInputText"
          label="Text to type"
          isRequired={true}
          isDisabled={isDisabled}
          hintText="Typed literally. To press Enter or Tab, add a Send Keys step after this one."
        />
      )}

      {isCombination && (
        <div className="mt-3">
          <LabelComponent
            text="Keys to send"
            isRequired={true}
          />

          <div className="flex align-items-center gap-3 mt-2">
            {isCapturing ? (
              <>
                <Tag
                  severity="info"
                  value={heldKeys.length > 0 ? toDisplay(heldKeys) : "Press the keys..."}
                />
                <Button
                  type="button"
                  label="Cancel"
                  icon="pi pi-times"
                  onClick={cancelCapture}
                  className="p-button-text"
                />
              </>
            ) : (
              <>
                {combination ? (
                  <Tag
                    severity="success"
                    value={combination}
                  />
                ) : (
                  <LabelComponent
                    text="Nothing recorded yet."
                    size="sm"
                  />
                )}

                <Button
                  type="button"
                  label={combination ? "Record again" : "Record keys"}
                  icon="pi pi-circle-fill"
                  onClick={record}
                  disabled={isDisabled}
                  className="p-button-outlined"
                />
              </>
            )}
          </div>

          {/* Captured through the global hook, so it sees keys this window never would - and
              records them the way the engine reads them back. */}
          <LabelComponent
            text="Press the combination anywhere; letting a key go finishes it. Escape is recorded like any other key."
            size="sm"
            className="mt-2"
          />
        </div>
      )}
    </>
  );
}
