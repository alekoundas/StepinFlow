import { Dropdown } from "primereact/dropdown";
import { InputText } from "primereact/inputtext";

import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { CHECK_RULES, type GestureFields, ruleNeedsWords } from "@/features/recording/gesture/gesture";

interface Props {
  fields: GestureFields;
  onChange: (patch: Partial<GestureFields>) => void;

  /** What was read on what was clicked, when it has been. */
  readText: string | null;
}

const PLACEHOLDERS: Partial<Record<ConditionTypeEnum, string>> = {
  [ConditionTypeEnum.MATCHES_REGEX]: "Remove|Removed",
  [ConditionTypeEnum.GREATER_THAN]: "100",
  [ConditionTypeEnum.LESS_THAN]: "100",
  [ConditionTypeEnum.BETWEEN]: "10",
};

/** A check by its words: the rule they follow, and what they are compared with. */
export default function CheckRuleFieldsComponent({ fields, onChange, readText }: Props) {
  const rule = fields.conditionType;
  const placeholder = PLACEHOLDERS[rule] ?? (readText || "The words to wait for");

  return (
    <div className="flex flex-column gap-2">
      <label
        htmlFor="gesture-check-rule"
        className="font-medium"
      >
        The words read there
      </label>

      <div className="flex align-items-center flex-wrap gap-2">
        <Dropdown
          inputId="gesture-check-rule"
          value={rule}
          options={CHECK_RULES}
          optionLabel="label"
          optionValue="value"
          onChange={(e) => onChange({ conditionType: e.value })}
          style={{ width: 220 }}
        />

        {ruleNeedsWords(rule) && (
          <InputText
            aria-label="What they are compared with"
            value={fields.searchText}
            placeholder={placeholder}
            onChange={(e) => onChange({ searchText: e.target.value })}
            className="flex-1"
            style={{ minWidth: 120 }}
          />
        )}

        {rule === ConditionTypeEnum.BETWEEN && (
          <>
            <span className="text-color-secondary">and</span>
            <InputText
              aria-label="The upper number"
              value={fields.conditionTextEnd}
              placeholder="50"
              onChange={(e) => onChange({ conditionTextEnd: e.target.value })}
              style={{ width: 100 }}
            />
          </>
        )}
      </div>

      <span className="text-xs text-color-secondary">
        {readText ? `Read on what you clicked: “${readText}”. ` : ""}A pattern is a regular expression.
      </span>
    </div>
  );
}
