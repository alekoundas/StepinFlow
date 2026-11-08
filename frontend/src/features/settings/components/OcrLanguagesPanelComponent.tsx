import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import {
  useOcrLanguageMutations,
  useOcrLanguages,
} from "@/features/settings/hooks/use-ocr-languages";

export default function OcrLanguagesPanelComponent() {
  const [downloadingTag, setDownloadingTag] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: languages = [], isLoading } = useOcrLanguages();
  const { downloadLanguageMutation } = useOcrLanguageMutations();

  const handleDownload = async (tag: string) => {
    setError(null);
    setDownloadingTag(tag);

    try {
      await downloadLanguageMutation.mutateAsync(tag);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDownloadingTag(null);
    }
  };

  return (
    <Panel header="Text recognition languages">
      <LabelComponent
        text="Text is read on this computer, the same way on every system. Latin comes with the app and reads English and most European languages. A Read Text step in another script needs its reader: a few megabytes, downloaded once."
        size="sm"
        color="secondary"
      />

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start mt-3"
          text={error}
        />
      )}

      <div className="flex flex-column gap-2 mt-3">
        {isLoading && (
          <LabelComponent
            text="Reading the readers..."
            size="sm"
          />
        )}

        {languages.map((language) => (
          <div
            key={language.tag}
            className="flex align-items-center justify-content-between gap-3 p-2 border-round surface-100"
          >
            <div className="flex flex-column gap-1 min-w-0">
              <LabelComponent text={language.displayName} />
              <LabelComponent
                text={language.languages}
                size="xs"
                color="secondary"
              />
            </div>

            {language.isBuiltIn && (
              <Tag
                severity="info"
                value="Built in"
              />
            )}
            {!language.isBuiltIn && language.isInstalled && (
              <Tag
                severity="success"
                value="Downloaded"
              />
            )}
            {!language.isInstalled && (
              <Button
                type="button"
                label={downloadingTag === language.tag ? "Downloading..." : `Download, ${language.megabytes} MB`}
                icon="pi pi-download"
                loading={downloadingTag === language.tag}
                disabled={downloadingTag !== null}
                onClick={() => handleDownload(language.tag)}
                className="p-button-outlined p-button-sm flex-none"
              />
            )}
          </div>
        ))}
      </div>
    </Panel>
  );
}
