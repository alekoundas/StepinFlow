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
import { AiProviderEnum } from "@/shared/enums/backend/ai/ai-provider-enum";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { useSettingEditor } from "@/features/settings/hooks/use-setting-editor";
import { aiKeys, useAiModels } from "@/features/ai/hooks/use-ai";
import OllamaModelDownloadComponent from "@/features/ai/components/OllamaModelDownloadComponent";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import type { AiModelDto } from "@/shared/models/ai-models-dto";

/**
 * The AI settings, which are a form rather than a list: what you need to fill in depends on which
 * provider you picked, and the model list has to be fetched from that provider. The generic panel
 * renders settings that stand alone, and these do not.
 */
export default function AiSettingsPanelComponent() {
  const queryClient = useQueryClient();

  const { data: settings = [] } = useAppSettings();
  const { valueOf, edit, commit, commitNow } = useSettingEditor();

  const find = (key: AppSettingKeyEnum): AppSettingDto | undefined =>
    settings.find((x) => x.key === key);

  /** What is saved, which is both what a control falls back to and what an edit is compared against. */
  const savedOf = (key: AppSettingKeyEnum): string => find(key)?.value ?? "";

  const shownOf = (key: AppSettingKeyEnum): string => valueOf(key, savedOf(key));

  const commitOf = (key: AppSettingKeyEnum) => commit(key, savedOf(key));

  const provider = find(AppSettingKeyEnum.AI_PROVIDER)?.value ?? AiProviderEnum.NONE;
  const isOllama = provider === AiProviderEnum.OLLAMA;
  const isOpenAi = provider === AiProviderEnum.OPENAI;
  const isOff = provider === AiProviderEnum.NONE;

  const { data: available, isFetching: isFetchingModels } = useAiModels(!isOff);

  // A different provider offers different models, so the list has to be asked for again.
  useEffect(() => {
    queryClient.invalidateQueries({ queryKey: aiKeys.models() });
    queryClient.invalidateQueries({ queryKey: aiKeys.status() });
    queryClient.invalidateQueries({ queryKey: aiKeys.suggestions() });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [provider]);

  const models = available?.models ?? [];
  const model = shownOf(AppSettingKeyEnum.AI_MODEL);
  const selectedModel = models.find((x) => x.name === model);
  const hasVision = selectedModel?.capabilities?.includes("vision") === true;

  const isScreenContentAllowed =
    savedOf(AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT) === "true";

  return (
    <Panel header="AI">
      <LabelComponent
        text="Optional. With a provider set up, a failed run can be explained in plain language."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-3 mt-3">
        <Row
          label="AI provider"
          description="Ollama runs the model on this machine and nothing leaves it. OpenAI is faster and better, but needs a key and sends the run away."
        >
          <Dropdown
            value={provider}
            options={[AiProviderEnum.NONE, AiProviderEnum.OLLAMA, AiProviderEnum.OPENAI]}
            onChange={(e) => commitNow(AppSettingKeyEnum.AI_PROVIDER, e.value)}
            className="w-12rem"
          />
        </Row>

        {isOllama && (
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
        )}

        {isOpenAi && (
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
        )}

        {!isOff && (
          <Row
            label="Model"
            description={
              isOllama
                ? "The models you have downloaded with Ollama."
                : "Which model to ask. The cheaper ones are enough to read a run."
            }
          >
            <Dropdown
              value={model}
              options={models}
              optionLabel="name"
              optionValue="name"
              itemTemplate={(option: AiModelDto) => <ModelOption model={option} />}
              // A model that is set but no longer downloaded would otherwise vanish from the box.
              placeholder={isFetchingModels ? "Looking..." : model || "Choose a model"}
              emptyMessage="Nothing to choose from"
              onChange={(e) => commitNow(AppSettingKeyEnum.AI_MODEL, e.value)}
              className="w-18rem"
            />
          </Row>
        )}

        {/* Under the dropdown it is about, and only once a model is chosen that cannot do it. */}
        {!isOff && model && selectedModel && !hasVision && (
          <div className="flex justify-content-end -mt-2">
            <LabelComponent
              text="Cannot read images, so run screenshots will not be sent."
              size="xs"
              color="warning"
            />
          </div>
        )}

        <OllamaModelDownloadComponent isEnabled={isOllama} />

        {available?.error && (
          <Message
            severity="warn"
            text={available.error}
          />
        )}

        {!isOff && (
          <Row
            label="Let the model see your screen"
            description={
              isOllama
                ? "On, and not changeable. The model runs here, so the text a Read Text step found and the screenshots a run kept never leave this machine."
                : "Off by default. Text a Read Text step found could be an account number or a password, and a screenshot is whatever was on screen. Turn this on only if you accept sending that to a provider outside this machine."
            }
          >
            <InputSwitch
              checked={isOllama || isScreenContentAllowed}
              disabled={isOllama}
              onChange={(e) =>
                commitNow(
                  AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT,
                  e.value ? "true" : "false",
                )
              }
            />
          </Row>
        )}

      </div>
    </Panel>
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
