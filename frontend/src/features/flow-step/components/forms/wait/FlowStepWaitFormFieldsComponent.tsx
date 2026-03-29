import { useState } from "react";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepWaitFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const [numberHintText, setNumberHintText] = useState<string>("");
  const setHintText = (value: number | null): void => {
    if (value) {
      // Use pipe 0 to floor numbers 13.6 | 0;  ==> 13

      // let leftOverMS:number = value
      const days = (value / (1000 * 60 * 60 * 24)) | 0;
      const hours = (value / (1000 * 60 * 60)) | 0;
      const minutes = (value / (1000 * 60)) | 0;
      const seconds = (value / 1000) | 0;

      const secondsText = seconds ? "Secs: " + seconds : "";
      const minutesText = minutes ? "Mins: " + seconds : "";
      const hoursText = hours ? "Hours: " + seconds : "";
      const daysText = days ? "Days: " + seconds : "";

      const values: string = [daysText, hoursText, minutesText, secondsText]
        .filter((x) => x.length > 0)
        .join(", ");

      setNumberHintText(values);
    }
  };

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormInputNumberComponent
        fieldName="waitForMilliseconds"
        label="Wait duration (ms)"
        hintText={numberHintText}
        min={50}
        max={2147483647} // signed int32 Max
        isRequired={true}
        isDisabled={isDisabled}
        onChanged={setHintText}
      />
    </>
  );
}
