import { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { Button } from "primereact/button";
import { useNavigate } from "react-router-dom";

export default function HomePage() {
  function uint8ArrayToDataURL(
    uint8arr: Uint8Array,
    mimeType = "image/png",
  ): string {
    let binary = "";
    const bytes = new Uint8Array(uint8arr);
    const len = bytes.byteLength;

    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }

    return `data:${mimeType};base64,${btoa(binary)}`;
  }

  const onGreet1 = async () => {
    const isok =
      await ElectronApiService.backendApi.System.inputRecordOverlayStart();
    console.log("Overlay start result:", isok);
  };
  const onGreet2 = async () => {
    const isok =
      await ElectronApiService.backendApi.System.inputRecordOverlayStop();
    console.log("Overlay stop result:", isok);
  };

  // const navigate = useNavigate();
  const onGreet3 = async () => {
    const sss = await ElectronApiService.backendApi.System.takeScreenshot(
      new ScreenshotRequestDto({
        captureVirtualScreen: true,
        formatType: "PNG",
      }),
    );
    if (sss) {
      // Step 2: Open image editor window
      // This returns when user clicks Export or closes window
      const result = await ElectronApiService.imageEditor.openWindow(sss);

      // Step 3: Handle result
      // if (result.pngBytes && result.pngBytes.length > 0) {
      //   // User clicked Export - use edited image
      //   console.log(
      //     `[IMAGE_SEARCH] Template edited, new size: ${result.pngBytes.length} bytes`,
      //   );
      //   return {
      //     success: true,
      //     editedImageBytes: result.pngBytes,
      //   };
      // } else {
      //   // User canceled - return original
      //   console.log("[IMAGE_SEARCH] Template edit canceled");
      //   return {
      //     success: false,
      //     editedImageBytes: currentImageBytes,
      //   };
      // }
      // }
    }
  };

  const onGreet4 = async () => {
    const sss = ElectronApiService.backendApi.System.takeScreenshot(
      new ScreenshotRequestDto({ captureVirtualScreen: true }),
    );
    sss.then((x) => console.log(x));
  };

  return (
    <div className="m-4">
      <h2>StepInFlow</h2>
      <p>Status: {status}</p>

      <Button
        label="start record input overlay"
        onClick={onGreet1}
        className="mb-4 p-button-success"
      />

      <Button
        label="stop record input overlay"
        onClick={onGreet2}
        className="mb-4 p-button-success"
      />
      <Button
        label="open editor window"
        onClick={onGreet3}
        className="mb-4 p-button-success"
      />
      <Button
        label="take screenshot"
        onClick={onGreet4}
        className="mb-4 p-button-success"
      />

      <h3>Received from .NET:</h3>
    </div>
  );
}
