import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Panel } from "primereact/panel";
import { Dropdown } from "primereact/dropdown";
import { InputText } from "primereact/inputtext";
import { Password } from "primereact/password";
import { InputSwitch } from "primereact/inputswitch";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { AiCloudProviderEnum } from "@/shared/enums/backend/ai/ai-cloud-provider-enum";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { useSettingEditor } from "@/features/settings/hooks/use-setting-editor";
import { aiKeys, useAiModels, useAiStatus } from "@/features/ai/hooks/use-ai";
import OllamaModelDownloadComponent from "@/features/ai/components/OllamaModelDownloadComponent";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import type { AiModelDto } from "@/shared/models/ai-models-dto";

/**
 * The AI settings, which are a form rather than a list: the model lists have to be fetched, and
 * what the cloud half asks for depends on whether a cloud model is wanted at all. The generic
 * panel renders settings that stand alone, and these do not.
 *
 * The local half always shows, because every AI feature is off without it. The cloud half is on
 * top: once it has a model and a key, that model answers in the local one's place.
 */
export default function AiSettingsPanelComponent() {
  const queryClient = useQueryClient();

  const { data: settings = [] } = useAppSettings();
  const { valueOf, edit, commit, commitNow } = useSettingEditor();
  const { data: isConfigured } = useAiStatus();
  const { data: available, isFetching: isFetchingModels } = useAiModels(true);

  const find = (key: AppSettingKeyEnum): AppSettingDto | undefined =>
    settings.find((x) => x.key === key);

  /** What is saved, which is both what a control falls back to and what an edit is compared against. */
  const savedOf = (key: AppSettingKeyEnum): string => find(key)?.value ?? "";

  const shownOf = (key: AppSettingKeyEnum): string => valueOf(key, savedOf(key));

  const commitOf = (key: AppSettingKeyEnum) => commit(key, savedOf(key));

  const ollamaUrl = savedOf(AppSettingKeyEnum.AI_OLLAMA_URL);
  const ollamaModel = savedOf(AppSettingKeyEnum.AI_OLLAMA_MODEL);
  const cloudProvider = savedOf(AppSettingKeyEnum.AI_CLOUD_PROVIDER) || AiCloudProviderEnum.NONE;
  const cloudModel = savedOf(AppSettingKeyEnum.AI_CLOUD_MODEL);
  const isCloud = cloudProvider !== AiCloudProviderEnum.NONE;

  // Whether AI is on, what can be chosen and what the chat can do all move with these.
  useEffect(() => {
    queryClient.invalidateQueries({ queryKey: aiKeys.models() });
    queryClient.invalidateQueries({ queryKey: aiKeys.status() });
    queryClient.invalidateQueries({ queryKey: aiKeys.suggestions() });
    queryClient.invalidateQueries({ queryKey: aiKeys.chatAvailability() });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ollamaUrl, ollamaModel, cloudProvider, cloudModel]);

  const models = available?.models ?? [];
  const selectedModel = models.find((x) => x.name === ollamaModel);
  const hasVision = selectedModel?.capabilities?.includes("vision") === true;

  const isScreenContentAllowed = savedOf(AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT) === "true";

  return (
    <Panel header="AI">
      <LabelComponent
        text="Every AI feature runs on a model on this machine, through Ollama. Install Ollama from ollama.com, start it, and choose a model below; until then they are off."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-3 mt-3">
        {isConfigured !== undefined && (
          <LocalStatusComponent
            error={available?.error ?? ""}
            model={ollamaModel}
            isConfigured={isConfigured}
          />
        )}

        <LabelComponent
          text="On this machine"
          weight="semibold"
        />

        <Row
          label="Ollama address"
          description="Where Ollama is listening. The default is right unless you moved it."
        >
          <InputText
            value={shownOf(AppSettingKeyEnum.AI_OLLAMA_URL)}
            onChange={(e) => edit(AppSettingKeyEnum.AI_OLLAMA_URL, e.target.value)}
            onBlur={() => commitOf(AppSettingKeyEnum.AI_OLLAMA_URL)}
            className="w-14rem"
          />
        </Row>

        <Row
          label="Model"
          description="The models you have downloaded with Ollama."
        >
          <Dropdown
            value={ollamaModel}
            options={models}
            optionLabel="name"
            optionValue="name"
            itemTemplate={(option: AiModelDto) => <ModelOption model={option} />}
            // A model that is set but no longer downloaded would otherwise vanish from the box.
            placeholder={isFetchingModels ? "Looking..." : ollamaModel || "Choose a model"}
            emptyMessage="Nothing to choose from"
            onChange={(e) => commitNow(AppSettingKeyEnum.AI_OLLAMA_MODEL, e.value)}
            className="w-18rem"
          />
        </Row>

        {/* Under the dropdown it is about, and only once a model is chosen that cannot do it. */}
        {!isCloud && selectedModel && !hasVision && (
          <div className="flex justify-content-end -mt-2">
            <LabelComponent
              text="Cannot read images, so run screenshots will not be sent."
              size="xs"
              color="warning"
            />
          </div>
        )}

        <OllamaModelDownloadComponent />

        <LabelComponent
          text="In the cloud"
          weight="semibold"
          className="mt-3"
        />

        <Row
          label="Cloud model"
          description="Optional. Once it has a model and a key it answers in the local model's place: faster and better, but it sends the run away."
        >
          <Dropdown
            value={cloudProvider}
            options={[AiCloudProviderEnum.NONE, AiCloudProviderEnum.OPENAI]}
            onChange={(e) => commitNow(AppSettingKeyEnum.AI_CLOUD_PROVIDER, e.value)}
            className="w-12rem"
          />
        </Row>

        {isCloud && (
          <>
            <Row
              label="API key"
              description="Kept on this machine and sent only to OpenAI."
            >
              <Password
                value={shownOf(AppSettingKeyEnum.AI_API_KEY)}
                placeholder={find(AppSettingKeyEnum.AI_API_KEY)?.isSet ? "Already set" : "Not set"}
                onChange={(e) => edit(AppSettingKeyEnum.AI_API_KEY, e.target.value)}
                onBlur={() => commitOf(AppSettingKeyEnum.AI_API_KEY)}
                feedback={false}
                toggleMask
                className="w-14rem"
              />
            </Row>

            <Row
              label="Model"
              description="Which model to ask. The cheaper ones are enough to read a run."
            >
              <Dropdown
                value={cloudModel}
                options={available?.cloudModels ?? []}
                optionLabel="name"
                optionValue="name"
                itemTemplate={(option: AiModelDto) => <ModelOption model={option} />}
                placeholder={cloudModel || "Choose a model"}
                onChange={(e) => commitNow(AppSettingKeyEnum.AI_CLOUD_MODEL, e.value)}
                className="w-18rem"
              />
            </Row>

            <Row
              label="Let the cloud model see your screen"
              description="Off by default. Text a Read Text step found could be an account number or a password, and a screenshot is whatever was on screen. Turn this on only if you accept sending that to a provider outside this machine. The local model always sees it: nothing leaves this machine."
            >
              <InputSwitch
                checked={isScreenContentAllowed}
                onChange={(e) => commitNow(AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT, e.value ? "true" : "false")}
              />
            </Row>
          </>
        )}
      </div>
    </Panel>
  );
}

interface LocalStatusProps {
  error: string;
  model: string;
  isConfigured: boolean;
}

/** Whether the AI features are on, and if not, the one thing in the way. */
function LocalStatusComponent({ error, model, isConfigured }: LocalStatusProps) {
  let severity: "success" | "warn" | "info" = "success";
  let text = `On. ${model} is ready on this machine.`;

  if (!isConfigured) {
    severity = "warn";
    text = `Off. Ollama has no ${model}: choose a model it has downloaded, or download one below.`;
  }
  if (!model) {
    severity = "info";
    text = "Off. Choose a model, or download one below.";
  }
  if (error) {
    severity = "warn";
    text = `Off. ${error}`;
  }

  return (
    <Message
      severity={severity}
      text={text}
      className="justify-content-start"
    />
  );
}

interface RowProps {
  label: string;
  description: string;
  children: React.ReactNode;
}

function Row({ label, description, children }: RowProps) {
  return (
    <div className="flex align-items-center justify-content-between gap-3">
      <div className="flex flex-column">
        <LabelComponent text={label} />
        <LabelComponent
          text={description}
          size="xs"
          color="secondary"
        />
      </div>

      {children}
    </div>
  );
}

interface ModelOptionProps {
  model: AiModelDto;
}

/**
 * A model and its badges.
 *
 * Which model to pick is otherwise a guess: tools decides whether it can answer questions about
 * your flows at all, and vision decides whether it can be shown a screenshot.
 */
function ModelOption({ model }: ModelOptionProps) {
  const capabilities = model.capabilities ?? [];
  const shown = ["tools", "vision", "thinking"].filter((x) =>
    capabilities.includes(x),
  );

  return (
    <div className="flex align-items-center justify-content-between gap-2 w-full">
      <LabelComponent text={model.name} />

      <div className="flex align-items-center gap-1">
        {shown.map((capability) => (
          <CapabilityBadge
            key={capability}
            text={capability}
          />
        ))}

        {model.contextLength > 0 ? (
          <CapabilityBadge text={`${Math.round(model.contextLength / 1024)}K`} />
        ) : null}
      </div>
    </div>
  );
}

interface CapabilityBadgeProps {
  text: string;
}

function CapabilityBadge({ text }: CapabilityBadgeProps) {
  return (
    <span
      style={{
        padding: "0.1rem 0.4rem",
        borderRadius: 4,
        border: "1px solid var(--surface-border)",
        background: "var(--surface-ground)",
        color: "var(--text-color-secondary)",
        fontSize: "0.65rem",
        whiteSpace: "nowrap",
      }}
    >
      {text}
    </span>
  );
}
