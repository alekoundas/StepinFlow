import { Tag } from "primereact/tag";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { TextSearchTestResultDto } from "@/shared/models/database/text-search-test-result-dto";

interface Props {
  result: TextSearchTestResultDto;
}

export default function FlowStepTextSearchTestPanelComponent({ result }: Props) {
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
        value={result.isMatch ? "Matched" : "No match"}
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
            text="After extraction"
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
