import { Tag } from "primereact/tag";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { SearchTextTestResultDto } from "@/shared/models/database/search-text-test-result-dto";

interface Props {
  result: SearchTextTestResultDto;
}

export default function FlowStepSearchTextTestPanelComponent({ result }: Props) {
  if (!result.isResolved) {
    return (
      <Panel
        header="Test result"
        className="mt-3"
      >
        <LabelComponent
          text={result.errorMessage ?? "The area could not be read."}
          color="error"
          size="sm"
        />
      </Panel>
    );
  }

  return (
    <Panel
      header="Test result"
      className="mt-3"
    >
      <Tag
        severity={result.isMatch ? "success" : "danger"}
        value={result.isMatch ? "Condition holds" : "Condition does not hold"}
      />

      {/* The whole read, so a near miss shows itself instead of just failing. */}
      <div className="mt-3">
        <LabelComponent
          text="Text read"
          weight="bold"
          size="sm"
        />
        <pre className="m-0 mt-1 p-2 surface-100 border-round text-sm overflow-auto max-h-15rem white-space-pre-wrap">
          {result.text.length > 0 ? result.text : "Nothing was read in this area."}
        </pre>
      </div>

      {result.resultValue !== result.text && (
        <div className="mt-3">
          <LabelComponent
            text="Kept"
            weight="bold"
            size="sm"
          />
          <pre className="m-0 mt-1 p-2 surface-100 border-round text-sm overflow-auto white-space-pre-wrap">
            {result.resultValue.length > 0
              ? result.resultValue
              : "The pattern matched nothing."}
          </pre>
        </div>
      )}
    </Panel>
  );
}
