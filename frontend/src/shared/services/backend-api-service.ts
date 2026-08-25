import type { IpcBroadcastMessage, IpcRequestMessage } from "../../../../electron/shared/types";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowValidationResultDto } from "@/shared/models/database/flow-validation-result-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { TreeNodeRequestDto } from "@/shared/models/tree-node-request.dto";
import type {
  FlowStepMoveDto,
  FlowStepMovePreviewDto,
} from "@/shared/models/flow-step-move.dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowAreaPreviewDto } from "@/shared/models/database/flow-area-preview-dto";
import type { ImageSearchTestResultDto } from "@/shared/models/database/image-search-test-result-dto";
import type { RunCommandTestResultDto } from "@/shared/models/database/run-command-test-result-dto";
import type { ReadTextTestResultDto } from "@/shared/models/database/read-text-test-result-dto";
import type { OcrLanguageDto } from "@/shared/models/database/ocr-language-dto";
import type { OcrLanguageInstallResultDto } from "@/shared/models/database/ocr-language-install-result-dto";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import type { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import type {
  FlowDraftDto,
  FlowDraftResultDto,
} from "@/shared/models/database/flow-draft-dto";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { CommandPresetDto } from "@/shared/models/database/command-preset-dto";
import type { LookupRequestDto } from "@/shared/models/lazy-data/lookup-request.dto";
import type { LookupResponseDto } from "@/shared/models/lazy-data/lookup-response.dto";
import type { LookupItemDto } from "@/shared/models/lazy-data/lookup-item.dto";
import type { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";
import type {
  WindowMatchTestRequestDto,
  WindowMatchTestResultDto,
} from "@/shared/models/window-match-dto";
import type {
  ExtractSubFlowDto,
  ExtractSubFlowResultDto,
} from "@/shared/models/database/extract-sub-flow-dto";
import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";
import type { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import type { ScreenPointDto } from "@/shared/models/screen-point.dto";

export const backendApiService = {
  greet: (name: string) => call<{ greeting: string }>("greet", { name }),

  Flow: {
    create: (dto: FlowDto) => call<number>("Flow.create", dto),
    update: (dto: FlowDto) => call<FlowDto>("Flow.update", dto),
    delete: (id: number) => call<boolean>("Flow.delete", id),

    get: (id: number) => call<FlowDto>("Flow.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowDto>>("Flow.getLazy", dto),
    getTreeNodes: (id: number) => call<TreeNodeDto[]>("Flow.getTreeNodes", id),
    validate: (id: number) =>
      call<FlowValidationResultDto>("Flow.validate", id),

    getCallers: (id: number) => call<LookupItemDto[]>("Flow.getCallers", id),
    getHealth: (flowIds: number[]) =>
      call<FlowHealthDto[]>("Flow.getHealth", { flowIds }),
    promoteToSubFlow: (id: number) => call<boolean>("Flow.promoteToSubFlow", id),
    extractSubFlow: (dto: ExtractSubFlowDto) =>
      call<ExtractSubFlowResultDto>("Flow.extractSubFlow", dto),
  },

  DiscordBot: {
    create: (dto: DiscordBotDto) => call<number>("DiscordBot.create", dto),
    update: (dto: DiscordBotDto) => call<DiscordBotDto>("DiscordBot.update", dto),
    delete: (id: number) => call<boolean>("DiscordBot.delete", id),

    get: (id: number) => call<DiscordBotDto>("DiscordBot.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<DiscordBotDto>>("DiscordBot.getLazy", dto),

    /** Sends immediately, skipping the throttle. Takes form values so an unsaved URL can be checked. */
    test: (dto: { webhookUrl: string; botName: string; avatarUrl: string }) =>
      call<boolean>("DiscordBot.test", dto),
  },

  FlowStep: {
    create: (dto: FlowStepDto) => call<number>("FlowStep.create", dto),
    createMany: (dto: FlowDraftDto) =>
      call<FlowDraftResultDto>("FlowStep.createMany", dto),
    update: (dto: FlowStepDto) => call<FlowStepDto>("FlowStep.update", dto),
    delete: (id: number) => call<boolean>("FlowStep.delete", id),

    get: (id: number) => call<FlowStepDto>("FlowStep.get", id),
    getDataTable: (dto: LazyDto) =>
      call<LazyResponseDto<FlowStepDto>>("FlowStep.getLazy", dto),
    getTreeNodes: (dto: TreeNodeRequestDto) =>
      call<TreeNodeDto[]>("FlowStep.getTreeNodes", dto),
    getMovePreview: (dto: FlowStepMoveDto) =>
      call<FlowStepMovePreviewDto>("FlowStep.getMovePreview", dto),
    move: (dto: FlowStepMoveDto) => call<boolean>("FlowStep.move", dto),
    testImageSearch: (dto: FlowStepDto) =>
      call<ImageSearchTestResultDto>("FlowStep.testImageSearch", dto),
    testRunCommand: (dto: FlowStepDto) =>
      call<RunCommandTestResultDto>("FlowStep.testRunCommand", dto),
    testReadText: (dto: FlowStepDto) =>
      call<ReadTextTestResultDto>("FlowStep.testReadText", dto),
  },

  FlowStepImage: {
    create: (dto: FlowStepImageDto) =>
      call<number>("FlowStepImage.create", dto),
    get: (id: number) => call<FlowStepImageDto>("FlowStepImage.get", id),
  },

  FlowArea: {
    create: (dto: FlowAreaDto) =>
      call<number>("FlowArea.create", dto),
    update: (dto: FlowAreaDto) =>
      call<FlowAreaDto>("FlowArea.update", dto),
    delete: (id: number) => call<boolean>("FlowArea.delete", id),
    get: (id: number) => call<FlowAreaDto>("FlowArea.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowAreaDto>>("FlowArea.getLazy", dto),
    getPreview: (id: number) =>
      call<FlowAreaPreviewDto>("FlowArea.getPreview", id),
  },

  FlowPoint: {
    create: (dto: FlowPointDto) => call<number>("FlowPoint.create", dto),
    update: (dto: FlowPointDto) =>
      call<FlowPointDto>("FlowPoint.update", dto),
    delete: (id: number) => call<boolean>("FlowPoint.delete", id),
    get: (id: number) => call<FlowPointDto>("FlowPoint.get", id),
    getPreview: (id: number) =>
      call<ScreenPointDto>("FlowPoint.getPreview", id),
  },

  Lookup: {
    window: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.window", dto),
    monitor: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.monitor", dto),
    flowStep: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowStep", dto),
    flowPoint: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowPoint", dto),
    flowArea: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowArea", dto),
    subFlow: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.subFlow", dto),
    discordBot: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.discordBot", dto),

    /** The mirror of flowStep: steps whose failure can be reported from where a Notify step sits. */
    failedStep: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.failedStep", dto),

    /** Runs a window matcher against this machine and returns every window it hits. */
    testWindowMatch: (dto: WindowMatchTestRequestDto) =>
      call<WindowMatchTestResultDto>("Lookup.testWindowMatch", dto),

    commandPresets: () => call<CommandPresetDto[]>("Lookup.commandPresets"),
    ocrLanguages: () => call<OcrLanguageDto[]>("Lookup.ocrLanguages"),
  },


  Recording: {
    start: () => call<boolean>("Recording.start"),
    stop: () => call<RecordedActionDto[]>("Recording.stop"),
    discard: () => call<boolean>("Recording.discard"),
    // .Net returns byte[], which arrives here as a base64 string
    getScreenshot: (index: number) =>
      call<string>("Recording.getScreenshot", index),
  },

  Settings: {
    getAll: () => call<AppSettingDto[]>("Settings.getAll"),
    set: (key: AppSettingKeyEnum, value: string) =>
      call<boolean>("Settings.set", { key, value }),
  },

  System: {
    // .Net returns byte[], which arrives here as a base64 string
    takeScreenshot: (dto: ScreenshotRequestDto) =>
      call<string>("System.takeScreenshot", dto),
    moveCursor: (dto: ScreenPointDto) =>
      call<boolean>("System.moveCursor", dto),
    installOcrLanguage: (languageTag: string) =>
      call<OcrLanguageInstallResultDto>("System.installOcrLanguage", languageTag),
    openWindowsLanguageSettings: () =>
      call<boolean>("System.openWindowsLanguageSettings"),
    inputRecordOverlayStart: () =>
      call<boolean>("System.inputRecordOverlayStart"),
    inputRecordOverlayStop: () =>
      call<boolean>("System.inputRecordOverlayStop"),
    inputRecordPointCaptureStart: () =>
      call<boolean>("System.inputRecordPointCaptureStart"),
    inputRecordPointCaptureStop: () =>
      call<boolean>("System.inputRecordPointCaptureStop"),
    inputRecordHotkeyStart: () => call<boolean>("System.inputRecordHotkeyStart"),
    inputRecordHotkeyStop: () => call<boolean>("System.inputRecordHotkeyStop"),
  },

  OnBroadcast: (callback: (msg: IpcBroadcastMessage<any>) => void): (() => void) => {
    return window.electronApi.backendApi.onBroadcast(callback);
  },
};

async function call<T = any>(action: string, payload: any = {}): Promise<T> {
  const msg: IpcRequestMessage = {
    action,
    payload,
  };

  const resultDto = await window.electronApi.backendApi.invoke<T>(msg);

  // Only null/undefined mean "no payload". A legitimate 0, false or "" is a value.
  if (!resultDto.isSuccess || resultDto.data === undefined || resultDto.data === null) {
    console.error(`Backend call failed [${action}]`, resultDto?.errorMessage);
    throw new Error(resultDto?.errorMessage ?? `Backend call failed [${action}]`);
  }

  return resultDto.data;
}
