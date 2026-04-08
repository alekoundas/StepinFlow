import { BrowserWindow, ipcMain } from "electron";
import net from "net";
import log from "electron-log";
import { IpcHandlerService } from "../IpcHandlerService.js";
import { ProtobufService } from "../protobuf/protobuf.js";
import { IPC_CHANNELS } from "../shared/channels.js";
import type { RequestMessage } from "../shared/types.js";

/**
 * Registers the `send-to-backend` ipcMain handler.
 * Extracted from main.ts so main stays lean.
 *
 * @param getClient  Getter for the current pipe socket (may be null)
 * @param setClient  Setter so reconnect can update the reference
 * @param pendingResponses  Shared correlation map
 * @param mainWindow  For reconnect 
 */
export async function registerBackendHandler(
  getClient: () => net.Socket | null,
  setClient: (client: net.Socket | null) => void,
  pendingResponses: Map<string, (plain: any) => void>,
  mainWindow: BrowserWindow | null,
): Promise<void> {
  const { IpcRequest } = await ProtobufService().getMessageTypes();

  ipcMain.handle(IPC_CHANNELS.BACKEND_SEND, async (_, msg: RequestMessage) => {
    console.log("[BackendHandler] Sending to backend:", msg);

    let client = getClient();

    if (!client || !client.writable) {
      log.log("[BackendHandler] Pipe not connected — reconnecting...");
      try {
        client = await IpcHandlerService().connectToDotNetPipe(
          mainWindow,
          setClient,
          pendingResponses,
        );
      } catch (reconnectErr) {
        log.error("[BackendHandler] Reconnect failed:", reconnectErr);
        throw new Error("[BackendHandler] Backend pipe not available");
      }
    }

    const payloadBytes = Buffer.from(JSON.stringify(msg.payload));
    const correlationId = crypto.randomUUID();

    const reqObj = {
      action: msg.action,
      payload: payloadBytes,
      correlationId,
    };

    const err = IpcRequest.verify(reqObj);
    if (err) throw new Error(err);

    const message = IpcRequest.create(reqObj);
    const buffer = IpcRequest.encode(message).finish();

    const prefix = Buffer.alloc(4);
    prefix.writeUInt32BE(buffer.length, 0);
    client.write(Buffer.concat([prefix, buffer]));

    return new Promise((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        if (pendingResponses.has(correlationId)) {
          pendingResponses.delete(correlationId);
          reject(
            new Error(
              `[BackendHandler] Timeout waiting for response to "${msg.action}"`,
            ),
          );
        }
      }, 30_000);

      pendingResponses.set(correlationId, (plain: any) => {
        clearTimeout(timeoutId);
        if (plain.error) {
          reject(new Error(plain.error));
          return;
        }
        let payload = plain.payload;
        if (Buffer.isBuffer(payload)) {
          try {
            payload = JSON.parse(payload.toString("utf-8"));
          } catch {
            // keep raw Buffer
          }
        }
        resolve(payload);
      });
    });
  });
}