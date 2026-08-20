import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import {
  useOcrLanguageMutations,
  useOcrLanguages,
} from "@/features/settings/hooks/use-ocr-languages";

export default function OcrLanguagesPanelComponent() {
  const [installingTag, setInstallingTag] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: languages = [], isLoading } = useOcrLanguages(installingTag !== null);
  const { installLanguageMutation } = useOcrLanguageMutations();

  // The install outlives the call that started it, so it is the language list that says when it
  // is done rather than the mutation. Clearing it here is also what stops the polling.
  if (installingTag && languages.some((x) => x.tag === installingTag && x.isInstalled))
    setInstallingTag(null);

  const handleInstall = async (tag: string) => {
    setError(null);
    setInstallingTag(tag);

    try {
      const result = await installLanguageMutation.mutateAsync(tag);
      if (!result.isRunning) setInstallingTag(null);
    } catch (err) {
      setInstallingTag(null);
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <Panel header="Text recognition languages">
      <LabelComponent
        text="A Read Text step can only use a language Windows has a pack for. Installing one asks for administrator permission and can take a few minutes."
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
            text="Reading installed languages..."
            size="sm"
          />
        )}

        {languages.map((language) => (
          <div
            key={language.tag}
            className="flex align-items-center justify-content-between gap-3 p-2 border-round surface-100"
          >
            <div className="flex align-items-center gap-2">
              <LabelComponent text={language.displayName} />
              <LabelComponent
                text={language.tag}
                size="xs"
                color="secondary"
              />
            </div>

            {language.isInstalled ? (
              <Tag
                severity="success"
                value="Installed"
              />
            ) : (
              <Button
                type="button"
                label={installingTag === language.tag ? "Installing..." : "Install"}
                icon="pi pi-download"
                loading={installingTag === language.tag}
                disabled={installingTag !== null}
                onClick={() => handleInstall(language.tag)}
                className="p-button-outlined p-button-sm"
              />
            )}
          </div>
        ))}
      </div>

      <Button
        type="button"
        label="Open Windows language settings"
        icon="pi pi-external-link"
        onClick={() => backendApiService.System.openWindowsLanguageSettings()}
        className="p-button-text mt-3"
        tooltip="For when the install cannot run here"
        tooltipOptions={{ position: "top" }}
      />
    </Panel>
  );
}
