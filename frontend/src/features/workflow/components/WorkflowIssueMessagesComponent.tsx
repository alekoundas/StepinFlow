import { Message } from "primereact/message";

import { severityRank } from "@/features/flow/hooks/use-flow-validation";
import {
  ValidationSeverityEnum,
  type FlowValidationIssueDto,
} from "@/shared/models/database/flow-validation-result-dto";

interface Props {
  issues: FlowValidationIssueDto[];
}

const MESSAGE_SEVERITY = {
  [ValidationSeverityEnum.ERROR]: "error",
  [ValidationSeverityEnum.WARNING]: "warn",
  [ValidationSeverityEnum.INFO]: "info",
} as const;

/** What is wrong with the step or the flow on screen, worst first, above the form that fixes it. */
export function WorkflowIssueMessagesComponent({ issues }: Props) {
  if (issues.length === 0) return null;

  const sorted = [...issues].sort((a, b) => severityRank(a.severity) - severityRank(b.severity));

  return (
    <div className="flex flex-column gap-2 mb-3">
      {sorted.map((issue, index) => (
        <Message
          key={`${issue.code}-${index}`}
          severity={MESSAGE_SEVERITY[issue.severity]}
          text={issue.message}
          className="w-full justify-content-start"
        />
      ))}
    </div>
  );
}
