import { centreClickOffset } from "@/shared/utils/image-size";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";
import type { FlowAreaPreviewDto } from "@/shared/models/database/flow-area-preview-dto";
import type {
  ImageSearchTestImageDto,
  ImageSearchTestResultDto,
} from "@/shared/models/database/image-search-test-result-dto";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";
import {
  isImageEditorPoint,
  useWindowImageEditor,
} from "@/windows/image-editor/hooks/use-window-image-editor";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FlowStepSearchImageSchema } from "@/features/flow-step/components/forms/search-image/flow-step-search-image.zod";
import FlowStepSearchImageFormFieldsComponent from "@/features/flow-step/components/forms/search-image/FlowStepSearchImageFormFieldsComponent";
import { FlowStepTemplateListComponent } from "@/features/flow-step/components/forms/search-image/FlowStepTemplateListComponent";
import FlowStepSearchImageTestDialogComponent from "@/features/flow-step/components/forms/search-image/FlowStepSearchImageTestDialogComponent";
import { FlowTemplateAddDialogComponent } from "@/features/flow-step/components/forms/search-image/FlowTemplateAddDialogComponent";
import { FlowTemplateSharedDialogComponent } from "@/features/flow-step/components/forms/search-image/FlowTemplateSharedDialogComponent";
import { defaultAccuracyFor } from "@/features/flow-step/components/forms/shared/match-modes";
import { stepsSearchingFor, useFlowStepUses } from "@/features/workflow/hooks/use-flow-parts";

const TEST_DETAILS_ID = "image-search-test-details";
const ADD_TEMPLATE_ID = "image-search-add-template";
const SHARED_TEMPLATE_ID = "image-search-shared-template";

// What an edit changed about a picture, or null when the window was closed without one.
type PictureChange = (picture: FlowTemplateDto) => Promise<Partial<FlowTemplateDto> | null>;

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepSearchImageFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepSearchImageSchema>>({
    resolver: zodResolver(FlowStepSearchImageSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  // Templates are a list rather than form fields: they carry binary and are edited through
  // their own windows.
  const [images, setImages] = useState<FlowStepTemplateDto[]>(
    defaultValues.flowStepTemplates ?? [],
  );

  // Capturing a template opens a window and waits, so by the time it resolves this component has
  // re-rendered and anything read from the closure is from before. Updates are computed against
  // the ref instead, so a slow capture cannot drop what came before it.
  const imagesRef = useRef(images);

  // Every template edit goes through here so it lands in the form too. Without that the list is
  // invisible to react-hook-form, isDirty never flips, and Save stays disabled. Setting the real
  // value rather than a flag also means undoing a change goes back to clean.
  const applyImages = useCallback(
    (update: (previous: FlowStepTemplateDto[]) => FlowStepTemplateDto[]) => {
      const next = update(imagesRef.current);

      imagesRef.current = next;
      setImages(next);
      form.setValue("flowStepTemplates", next, { shouldDirty: true });
    },
    [form],
  );

  // A value tuned for one mode means nothing in the other - 0.80 under shape and brightness finds
  // a template on a blank screen - so changing the mode puts every template on the new mode's
  // default instead of carrying the number over. watch fires on changes only, never on load.
  useEffect(() => {
    const subscription = form.watch((values, { name }) => {
      if (name !== "templateMatchMode") return;

      const accuracy = defaultAccuracyFor(values.templateMatchMode);
      applyImages((previous) =>
        previous.map((x) => new FlowStepTemplateDto({ ...x, accuracy })),
      );
    });

    return () => subscription.unsubscribe();
  }, [form, applyImages]);
  const [testResult, setTestResult] = useState<ImageSearchTestResultDto | null>(
    null,
  );
  const [isTesting, setIsTesting] = useState(false);
  const [templateError, setTemplateError] = useState<string | null>(null);

  const { openWindow, isWindowOpen } = useWindowOverlay();
  const { openImageEditor } = useWindowImageEditor();
  const { openConfirm, openCustom, close } = useDialogStore();

  // Which other steps link to each picture, as the flow was last saved.
  const { data: uses } = useFlowStepUses(defaultValues.rootId || null);
  const sharedWith = images.map((x) =>
    x.flowTemplateId > 0 ? stepsSearchingFor(uses, x.flowTemplate.name, defaultValues.id) : [],
  );

  // Passed by value: the dialog store keeps elements, so a result read from the closure later
  // would be whatever it was at open time anyway.
  const openTestDetails = () => {
    if (!testResult) return;

    openConfirm(TEST_DETAILS_ID, {
      headerText: "What the search found",
      hideConfirm: true,
      cancelLabel: "Close",
      width: "min(60rem, 90vw)",
      children: <FlowStepSearchImageTestDialogComponent result={testResult} />,
      onConfirm: () => close(TEST_DETAILS_ID),
    });
  };

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const buildDto = (data?: z.infer<typeof FlowStepSearchImageSchema>) =>
    new FlowStepDto({
      ...defaultValues,
      ...(data ?? (form.getValues() as never)),
      flowAreaId: (data ?? form.getValues()).flowAreaId ?? undefined,
      flowStepTemplates: imagesRef.current,
    });

  // Capture a new picture, or link one the flow already has.
  const handleAddTemplate = () => {
    setTemplateError(null);

    openCustom(
      ADD_TEMPLATE_ID,
      <FlowTemplateAddDialogComponent
        flowId={defaultValues.rootId}
        stepName={form.getValues().name ?? ""}
        takenIds={imagesRef.current.map((x) => x.flowTemplateId).filter((x) => x > 0)}
        onCapture={handleCaptureTemplate}
        onReuse={reuseTemplate}
      />,
    );
  };

  // A template is measured against the area it is searched in - that area's size and DPI are
  // what let it scale on another screen - so there is no capturing one until the step has an
  // area. The drag is confined to it, because a template from outside it can never be found.
  const handleCaptureTemplate = async () => {
    setTemplateError(null);

    const areaId = form.getValues().flowAreaId;
    if (!areaId) {
      setTemplateError(
        "Pick where to look first. A template is measured against the area it is searched in.",
      );
      return;
    }

    // An area added in this same editing session only has a temporary negative id, so there is
    // nothing for the backend to resolve yet.
    if (areaId < 0) {
      setTemplateError(
        "Save the flow before capturing in that area, which has not been created yet.",
      );
      return;
    }

    try {
      const area = await backendApiService.FlowArea.getPreview(areaId);
      if (!area.isResolved) {
        setTemplateError(
          area.errorMessage ??
            "The search area is not on screen right now, so a template cannot be measured against it.",
        );
        return;
      }

      const selection = await openWindow(
        {
          x: area.locationX,
          y: area.locationY,
          width: area.width,
          height: area.height,
        },
        "Search area",
        true,
      );
      if (!selection) return;

      if (!selection.image) {
        setTemplateError("The template could not be cut from the screenshot. Capture it again.");
        return;
      }

      addTemplate(selection.image, area);
    } catch (err) {
      // Without this the capture fails and the list simply stays empty, which reads as the
      // button doing nothing.
      console.error(err);
      setTemplateError(
        err instanceof Error ? err.message : "The template could not be captured.",
      );
    }
  };

  // The name is only what the list shows until the save gives the picture its file name.
  const addTemplate = (screenshot: string, area: FlowAreaPreviewDto) => {
    applyImages((previous) => [
      ...previous,
      new FlowStepTemplateDto({
        accuracy: defaultAccuracyFor(form.getValues().templateMatchMode),
        flowTemplate: new FlowTemplateDto({
          name: `Template ${previous.length + 1}`,
          templateImage: screenshot,
          authoredFlowAreaWidth: area.width,
          authoredFlowAreaHeight: area.height,
          authoredDpi: area.dpi,
          ...centreClickOffset(screenshot),
        }),
      }),
    ]);
  };

  // The step links to the picture; the accuracy is its own, on the mode's default.
  const reuseTemplate = (template: FlowTemplateDto) => {
    applyImages((previous) => [
      ...previous,
      new FlowStepTemplateDto({
        accuracy: defaultAccuracyFor(form.getValues().templateMatchMode),
        flowTemplateId: template.id,
        flowTemplate: new FlowTemplateDto(template),
      }),
    ]);
  };

  // A copy has no id, so the save makes it a new picture of the flow and only this step links to it.
  const editPicture = async (index: number, asCopy: boolean, change: PictureChange) => {
    const image = imagesRef.current[index];
    const changed = await change(image.flowTemplate);
    if (!changed) return;

    if (!asCopy) {
      updateImage(
        index,
        new FlowStepTemplateDto({
          ...image,
          flowTemplate: new FlowTemplateDto({ ...image.flowTemplate, ...changed }),
        }),
      );
      return;
    }

    updateImage(
      index,
      new FlowStepTemplateDto({
        ...image,
        flowTemplateId: 0,
        flowTemplate: new FlowTemplateDto({
          ...image.flowTemplate,
          ...changed,
          id: 0,
          name: `Template ${index + 1}`,
          flowStepsCount: 0,
        }),
      }),
    );
  };

  // Changing a picture other steps link to asks first: for all of them, or a copy for this step.
  const askThenEdit = (index: number, change: PictureChange) => {
    const others = sharedWith[index] ?? [];
    if (others.length === 0) {
      editPicture(index, false, change);
      return;
    }

    openCustom(
      SHARED_TEMPLATE_ID,
      <FlowTemplateSharedDialogComponent
        template={imagesRef.current[index].flowTemplate}
        stepName={form.getValues().name ?? ""}
        otherSteps={others}
        onAll={() => editPicture(index, false, change)}
        onCopy={() => editPicture(index, true, change)}
      />,
    );
  };

  // Re-cropping moves every pixel, so a click point picked on the old image now points at
  // something else - or off the edge. The middle of the new one is the honest answer.
  const cropPicture: PictureChange = async (picture) => {
    if (!picture.templateImage) return null;

    const edited = await openImageEditor(picture.templateImage);
    if (typeof edited !== "string") return null;

    return { templateImage: edited, ...centreClickOffset(edited) };
  };

  const pickClickPoint: PictureChange = async (picture) => {
    if (!picture.templateImage) return null;

    const point = await openImageEditor(picture.templateImage, "PICK_POINT");
    if (!isImageEditorPoint(point)) return null;

    return { clickOffsetX: point.x, clickOffsetY: point.y };
  };

  const handleEditImage = (index: number) => askThenEdit(index, cropPicture);
  const handleSetClickPoint = (index: number) => askThenEdit(index, pickClickPoint);
  const handleEditCopy = (index: number) => editPicture(index, true, cropPicture);

  const updateImage = (index: number, image: FlowStepTemplateDto) =>
    applyImages((previous) => previous.map((x, i) => (i === index ? image : x)));

  const handleTest = async () => {
    setIsTesting(true);
    try {
      setTestResult(
        await backendApiService.FlowStep.testImageSearch(buildDto()),
      );
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
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit((data) => onSubmit(buildDto(data)))}>
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
          testAction={
            <div className="flex gap-2">
              <Button
                type="button"
                label="Test now"
                icon="pi pi-play"
                loading={isTesting}
                disabled={images.length === 0}
                onClick={handleTest}
                outlined
                tooltip="Searches the live screen without clicking anything"
                tooltipOptions={{ position: "top" }}
              />

              <Button
                type="button"
                label="Details"
                icon="pi pi-image"
                disabled={!testResult?.isResolved}
                onClick={openTestDetails}
                outlined
                severity="secondary"
                tooltip="See what matched, and where it would click"
                tooltipOptions={{ position: "top" }}
              />
            </div>
          }
        >
          <FlowStepSearchImageFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            isDisabled={formMode === "VIEW"}
          />

          <FlowStepTemplateListComponent
            images={images}
            testResults={testResultsByIndex}
            sharedWith={sharedWith}
            isDisabled={formMode === "VIEW" || isWindowOpen}
            onAdd={handleAddTemplate}
            onEditImage={handleEditImage}
            onSetClickPoint={handleSetClickPoint}
            onEditCopy={handleEditCopy}
            onChange={updateImage}
            onRemove={(index) =>
              applyImages((previous) => previous.filter((_, i) => i !== index))
            }
          />

          {templateError && (
            <Message
              severity="error"
              className="w-full justify-content-start mt-2"
              text={templateError}
            />
          )}

          {testResult && !testResult.isResolved && (
            <Message
              severity="error"
              className="w-full justify-content-start mt-3"
              text={testResult.errorMessage ?? "Could not resolve the search area."}
            />
          )}

          {testResult?.isResolved && (
            <Message
              severity={testResult.wouldSucceed ? "success" : "warn"}
              className="w-full justify-content-start mt-3"
              text={
                testResult.wouldSucceed
                  ? `Would succeed. ${testResult.totalMatches} match(es) in ${testResult.searchAreaWidth}×${testResult.searchAreaHeight}.`
                  : `Would go on to the Failure steps. ${testResult.totalMatches} match(es) found.`
              }
            />
          )}
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
