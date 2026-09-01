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
                ? "The models you have pulled with Ollama."
                : "Which model to ask. The cheaper ones are enough to read a run."
            }
          >
            <Dropdown
              value={model}
              options={models}
              // A model that is set but no longer pulled would otherwise vanish from the box.
              placeholder={isFetchingModels ? "Looking..." : model || "Choose a model"}
              emptyMessage="Nothing to choose from"
              onChange={(e) => commitNow(AppSettingKeyEnum.AI_MODEL, e.value)}
              className="w-14rem"
            />
          </Row>
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
            label="Send text read from the screen"
            description={
              isOllama
                ? "On, because the model runs here and nothing leaves this machine. It lets a failure say what was actually on screen."
                : "Off, and not changeable. Text a Read Text step found could be an account number or a password, so it is never sent to a provider outside this machine."
            }
          >
            <InputSwitch
              checked={isOllama}
              disabled
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
