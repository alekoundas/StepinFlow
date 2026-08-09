import { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { Button } from "primereact/button";
import { useState } from "react";

export default function HomePage() {
  // Temporary playground until the IMAGE_SEARCH flow step exists.
  const [status, setStatus] = useState("idle");
  const [templatePreview, setTemplatePreview] = useState<string | null>(null);

  const onStartRecording = async () => {
    const isOk =
      await ElectronApiService.backendApi.System.inputRecordOverlayStart();
    setStatus(`overlay recording started: ${isOk}`);
  };

  const onStopRecording = async () => {
    const isOk =
      await ElectronApiService.backendApi.System.inputRecordOverlayStop();
    setStatus(`overlay recording stopped: ${isOk}`);
  };

  const onOpenImageEditor = async () => {
    setStatus("capturing virtual screen...");

    // base64 PNG of the whole virtual desktop (all monitors)
    const screenshotBase64 =
      await ElectronApiService.backendApi.System.takeScreenshot(
        new ScreenshotRequestDto({
          captureVirtualScreen: true,
          formatType: "PNG",
        }),
      );

    if (!screenshotBase64) {
      setStatus("screenshot failed");
      return;
    }

    setStatus("editing...");
    const result = await ElectronApiService.imageEditor.openWindow({
      imageBase64: screenshotBase64,
      mode: "EDIT",
    });
    const editedBase64 = typeof result === "string" ? result : null;

    if (editedBase64) {
      setTemplatePreview(`data:image/png;base64,${editedBase64}`);
      setStatus(`template saved (${editedBase64.length} base64 chars)`);
    } else {
      setTemplatePreview(null);
      setStatus("editing cancelled");
    }
  };

  return (
    <div className="m-4">
      <h2>StepInFlow</h2>
      <p>Status: {status}</p>

      <div className="flex flex-wrap gap-2 mb-4">
        <Button
          label="start record input overlay"
          onClick={onStartRecording}
          className="p-button-success"
        />
        <Button
          label="stop record input overlay"
          onClick={onStopRecording}
          className="p-button-success"
        />
        <Button
          label="open editor window"
          onClick={onOpenImageEditor}
          className="p-button-success"
        />
      </div>

      {templatePreview && (
        <>
          <h3>Template returned from the editor:</h3>
          <img
            src={templatePreview}
            alt="edited template"
            style={{ maxWidth: "100%", border: "1px solid #444" }}
          />
        </>
      )}
    </div>
  );
}
