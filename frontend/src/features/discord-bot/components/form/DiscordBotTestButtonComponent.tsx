import { useEffect, useRef, useState } from "react";
import { useFormContext } from "react-hook-form";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";

/** Long enough that a double click cannot send twice, short enough to not feel broken. */
const COOLDOWN_MS = 2000;

interface Props {
  isDisabled?: boolean;
}

/**
 * Sends one message now, so a bad URL is caught when it is pasted rather than at 3am when a flow
 * fails.
 *
 * The throttle here is a disabled button, never a silent drop. A diagnostic control that accepts
 * the click and discards the message is the one place throttling does real damage, because the
 * only reasonable conclusion is that the webhook is broken.
 *
 * Deliberately not the bot's own rate limit: a bot set to 300 seconds would otherwise be
 * untestable. The backend skips the throttle for this call for the same reason.
 */
export default function DiscordBotTestButtonComponent({ isDisabled = false }: Props) {
  const { getValues, trigger } = useFormContext();

  const [isSending, setIsSending] = useState(false);
  const [isCoolingDown, setIsCoolingDown] = useState(false);
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);

  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(
    () => () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    },
    [],
  );

  const send = async () => {
    // The URL has to be a real webhook before there is any point posting to it.
    const isUrlValid = await trigger("webhookUrl");
    if (!isUrlValid) return;

    setIsSending(true);
    setResult(null);

    try {
      await backendApiService.DiscordBot.test({
        webhookUrl: getValues("webhookUrl"),
        botName: getValues("botName") ?? "",
        avatarUrl: getValues("avatarUrl") ?? "",
      });

      setResult({ ok: true, text: "Sent. Check the channel." });
    } catch (err) {
      setResult({ ok: false, text: err instanceof Error ? err.message : String(err) });
    } finally {
      setIsSending(false);
      setIsCoolingDown(true);
      timerRef.current = setTimeout(() => setIsCoolingDown(false), COOLDOWN_MS);
    }
  };

  return (
    <div className="flex flex-column gap-2">
      <div className="flex align-items-center gap-3">
        <Button
          type="button"
          label={isSending ? "Sending..." : "Send a test message"}
          icon="pi pi-send"
          loading={isSending}
          disabled={isDisabled || isSending || isCoolingDown}
          onClick={() => void send()}
          className="p-button-outlined p-button-sm"
        />

        {result && (
          <Message
            severity={result.ok ? "success" : "error"}
            text={result.text}
            className="justify-content-start"
          />
        )}
      </div>

      <LabelComponent
        text="One test every 2 seconds. This ignores the rate limit below."
        size="xs"
        color="secondary"
      />
    </div>
  );
}
