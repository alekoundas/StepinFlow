import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type {
  ImageSearchTestImageDto,
  ImageSearchTestResultDto,
} from "@/shared/models/database/image-search-test-result-dto";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";
import {
  isImageEditorPoint,
  useWindowImageEditor,
} from "@/windows/image-editor/hooks/use-window-image-editor";
import { FlowStepImageSearchSchema } from "@/features/flow-step/components/forms/image-search/flow-step-image-search.zod";
import FlowStepImageSearchFormFieldsComponent from "@/features/flow-step/components/forms/image-search/FlowStepImageSearchFormFieldsComponent";
import { FlowStepImageListComponent } from "@/features/flow-step/components/forms/image-search/FlowStepImageListComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepImageSearchFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepImageSearchSchema>>({
    resolver: zodResolver(FlowStepImageSearchSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  // Templates are a list rather than form fields: they carry binary and are edited through
  // their own windows.
  const [images, setImages] = useState<FlowStepImageDto[]>(
    defaultValues.flowStepImages ?? [],
  );
  const [testResult, setTestResult] = useState<ImageSearchTestResultDto | null>(null);
  const [isTesting, setIsTesting] = useState(false);

  const { openWindow, isWindowOpen } = useWindowOverlay();
  const { openImageEditor } = useWindowImageEditor();

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const buildDto = (data?: z.infer<typeof FlowStepImageSearchSchema>) =>
    new FlowStepDto({
      ...defaultValues,
      ...(data ?? (form.getValues() as never)),
      flowAreaId: (data ?? form.getValues()).flowAreaId ?? undefined,
      flowStepImages: images,
    });

  // Captured region becomes the template, and the frame it was captured in becomes the
  // scaling key so it still matches on a different resolution.
  const handleAddTemplate = async () => {
    const rect = await openWindow();
    if (!rect) return;

    const areaId = form.getValues().flowAreaId;
    let frameWidth = rect.width;
    let frameHeight = rect.height;

    if (areaId) {
      const preview = await backendApiService.FlowArea.getPreview(areaId);
      if (preview.isResolved) {
        frameWidth = preview.width;
        frameHeight = preview.height;
      }
    }

    const screenshot = await backendApiService.System.takeScreenshot({
      formatType: "PNG",
      jpegQuality: 100,
      locationX: rect.x,
      locationY: rect.y,
      width: rect.width,
      height: rect.height,
      captureVirtualScreen: false,
      captureMonitor: "",
      captureAppWindow: "",
    });

    setImages((prev) => [
      ...prev,
      new FlowStepImageDto({
        name: `Template ${prev.length + 1}`,
        templateImage: screenshot,
        authoredFrameWidth: frameWidth,
        authoredFrameHeight: frameHeight,
      }),
    ]);
  };

  const handleEditImage = async (index: number) => {
    const image = images[index];
    if (!image.templateImage) return;

    const edited = await openImageEditor(image.templateImage);
    if (typeof edited !== "string") return;

    updateImage(index, new FlowStepImageDto({ ...image, templateImage: edited }));
  };

  const handleSetClickPoint = async (index: number) => {
    const image = images[index];
    if (!image.templateImage) return;

    const point = await openImageEditor(image.templateImage, "PICK_POINT");
    if (!isImageEditorPoint(point)) return;

    updateImage(
      index,
      new FlowStepImageDto({
        ...image,
        clickOffsetX: point.x,
        clickOffsetY: point.y,
      }),
    );
  };

  const updateImage = (index: number, image: FlowStepImageDto) =>
    setImages((prev) => prev.map((x, i) => (i === index ? image : x)));

  const handleTest = async () => {
    setIsTesting(true);
    try {
      setTestResult(await backendApiService.FlowStep.testImageSearch(buildDto()));
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  const testResultsByIndex = new Map<number, ImageSearchTestImageDto>(
    (testResult?.images ?? []).map((x, index) => [index, x]),
  );

  return (
    <>
      <FormHeaderComponent
        title="Image Search Step Configuration"
        description="Look for one or more template images inside a search area, then branch on what was found."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((data) => onSubmit(buildDto(data)))}
          className="flex flex-column h-full"
        >
          <FlowStepImageSearchFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            templateCount={images.length}
            isDisabled={formMode === "VIEW"}
          />

          <FlowStepImageListComponent
            images={images}
            testResults={testResultsByIndex}
            isDisabled={formMode === "VIEW" || isWindowOpen}
            onAdd={handleAddTemplate}
            onEditImage={handleEditImage}
            onSetClickPoint={handleSetClickPoint}
            onChange={updateImage}
            onRemove={(index) =>
              setImages((prev) => prev.filter((_, i) => i !== index))
            }
          />

          <div className="flex gap-3 align-items-center mt-3">
            <Button
              type="button"
              label="Test now"
              icon="pi pi-play"
              loading={isTesting}
              disabled={images.length === 0}
              onClick={handleTest}
              className="p-button-outlined"
              tooltip="Run the search against the live screen without clicking anything"
              tooltipOptions={{ position: "top" }}
            />

            {testResult && !testResult.isResolved && (
              <Message
                severity="error"
                className="flex-1 justify-content-start"
                text={testResult.errorMessage ?? "Could not resolve the search area."}
              />
            )}

            {testResult?.isResolved && (
              <Message
                severity={testResult.wouldSucceed ? "success" : "warn"}
                className="flex-1 justify-content-start"
                text={
                  testResult.wouldSucceed
                    ? `Would succeed. ${testResult.totalMatches} match(es) in ${testResult.searchAreaWidth}×${testResult.searchAreaHeight}.`
                    : `Would run the Failure steps. ${testResult.totalMatches} match(es) found.`
                }
              />
            )}
          </div>

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </>
  );
}
