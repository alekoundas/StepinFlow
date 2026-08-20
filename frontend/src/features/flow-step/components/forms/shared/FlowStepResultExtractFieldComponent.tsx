import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";

interface Props {
  // What the step produces, so the hint can say it in the step's own words.
  resultDescription: string;
  isDisabled?: boolean;
}

/**
 * Narrows what a step hands to the Check Value steps that read it. Shared by every step that
 * produces text, so the rules stay identical wherever a result comes from.
 */
export default function FlowStepResultExtractFieldComponent({
  resultDescription,
  isDisabled = false,
}: Props) {
  return (
    <FormInputTextComponent
      fieldName="resultExtractPattern"
      label="Keep only"
      placeholderText="(\d+)"
      isDisabled={isDisabled}
      hintText={`Regex, first capture group. Empty keeps everything. ${resultDescription}`}
    />
  );
}
