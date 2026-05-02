import { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { Button } from "primereact/button";
import { useNavigate } from "react-router-dom";

export default function HomePage() {
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

  const navigate = useNavigate();
  const onGreet3 = async () => {
    const isok =
      await ElectronApiService.backendApi.System.inputRecordOverlayStart();
    console.log("Overlay start result:", isok);
    navigate("/search-area-overlay");
  };

  const onGreet4 = async () => {
    const sss = ElectronApiService.backendApi.System.takeScreenshot(
      new ScreenshotRequestDto({ isVirtualScreen: true }),
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
        label="stop record input overlay"
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
