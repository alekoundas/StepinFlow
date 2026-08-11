import { Tag } from "primereact/tag";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { RunCommandTestResultDto } from "@/shared/models/database/run-command-test-result-dto";

interface Props {
  result: RunCommandTestResultDto;
}

const OutputBlock = ({ label, text }: { label: string; text: string }) => (
  <div className="mt-3">
    <LabelComponent
      text={label}
      weight="bold"
      size="sm"
    />
    <pre className="m-0 mt-1 p-2 surface-100 border-round text-sm overflow-auto max-h-15rem white-space-pre-wrap">
      {text}
    </pre>
  </div>
);

export default function FlowStepSystemCommandTestPanelComponent({ result }: Props) {
  const hasOutput =
    result.standardOutput.length > 0 || result.standardError.length > 0;

  return (
    <Panel
      header="Test result"
      className="mt-3"
    >
      {result.errorMessage ? (
        <LabelComponent
          text={result.errorMessage}
          color="error"
          size="sm"
        />
      ) : (
        <div className="flex align-items-center gap-2">
          <Tag
            severity={result.isSuccess ? "success" : "danger"}
            value={`Exit code ${result.exitCode}`}
          />
          <LabelComponent
            text={`${result.durationMilliseconds} ms`}
            color="secondary"
            size="sm"
          />
        </div>
      )}

      {/* With presets and variables in play, what actually ran is the useful line. */}
      <OutputBlock
        label="Command"
        text={result.resolvedCommand}
      />

      {result.standardOutput.length > 0 && (
        <OutputBlock
          label="Output"
          text={result.standardOutput}
        />
      )}

      {result.standardError.length > 0 && (
        <OutputBlock
          label="Errors"
          text={result.standardError}
        />
      )}

      {!hasOutput && !result.errorMessage && (
        <LabelComponent
          text="The command produced no output."
          color="secondary"
          size="sm"
          className="mt-3"
        />
      )}
    </Panel>
  );
}
