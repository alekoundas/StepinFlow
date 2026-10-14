import { BrowserWindow, ipcMain, Net } from "electron";
import net from "net";
import log from "electron-log";
import { ProtobufService } from "../../protobuf/protobuf.js";
import { IPC_CHANNELS } from "../../shared/channels.js";
import type {
  IpcRequestMessage,
  IpcResponseMessage,
} from "../../shared/types.js";
import { BackendService } from "../services/backend-service.js";

export type InvokeBackend = (action: string, payload: unknown) => Promise<any>;
interface PendingResolver {
  resolve: (value: any) => void;
  reject: (reason: any) => void;
  timeoutId: ReturnType<typeof setTimeout>;
}
const { IpcRequest, IpcResponse } =
  await ProtobufService().getMessageTypes();

export async function registerBackendRequestHandler(
  mainWindow: BrowserWindow | null,
): Promise<{
  invokeBackend: InvokeBackend;
}> {
  const pending = new Map<string, PendingResolver>();
  let backendClient: net.Socket | null = null;

  const getClient = () => backendClient;
  const setClient = (socket: net.Socket | null) => (backendClient = socket);
  const onConnected = (socket: net.Socket) =>
    handleRequestPipeData(socket, pending);

  //============================================
  // Initial connection
  //
  // Deliberately not awaited. Handler registration must not depend on the backend being up:
  // main.ts awaits this function before registering the overlay and editor handlers, so
  // blocking here brings the app up with a clickable UI and no handlers behind it.
  // invokeBackend reconnects on demand anyway, so this is only a warm-up.
  //============================================
  void BackendService()
    .connectToRequestPipe(mainWindow, setClient, onConnected)
    .catch((err) =>
      log.error("[BackendHandler]: Initial connect failed, will retry on demand:", err),
    );

  //============================================
  // IPC handle: renderer -> invokeBackend -> .Net
  //============================================
  ipcMain.handle(
    IPC_CHANNELS.BACKEND_SEND,
    async (_, msg: IpcRequestMessage) => {
      console.log("[BackendHandler] Sending to backend:", msg);
      return invokeBackend(msg.action, msg.payload);
    },
  );

  //============================================
  // Expose reusable invoke .Net method (also used on other IpcHandlers in electron)
  //============================================
  const invokeBackend: InvokeBackend = async (
    action: string,
    payload: unknown,
  ): Promise<any> => {
    let client = getClient();

    if (!client || !client.writable) {
      log.log("[BackendHandler]: Pipe not connected — reconnecting...");
      try {
        backendClient = await BackendService().connectToRequestPipe(
          mainWindow,
          setClient,
          onConnected,
        );
        client = backendClient;
      } catch (err) {
        log.error("[BackendHandler]: Reconnect failed:", err);
        throw new Error("[BackendHandler]: Backend pipe not available");
      }
    }

    const correlationId = crypto.randomUUID();
    const payloadBytes = Buffer.from(JSON.stringify(payload));
    const reqObj = { action, payload: payloadBytes, correlationId };

    const verifyErr = IpcRequest.verify(reqObj);
    if (verifyErr) throw new Error(verifyErr);

    const encoded = IpcRequest.encode(IpcRequest.create(reqObj)).finish();
    const prefix = Buffer.alloc(4);
    prefix.writeUInt32BE(encoded.length, 0);
    getClient()!.write(Buffer.concat([prefix, encoded]));

    return new Promise<any>((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        if (pending.has(correlationId)) {
          pending.delete(correlationId);
          reject(new Error(`[BackendHandler] Timeout for "${action}"`));
        }
      }, 30_000);

      pending.set(correlationId, { resolve, reject, timeoutId });
    });
  };

  return { invokeBackend };
}

// Reads every IpcResponse in a chunk. Broadcasts have a pipe of their own, so nothing else comes
// here. A chunk can hold more than one response, and one left in the buffer would wait for the
// next chunk to be read - for ever, when no other request follows - and time out.
function handleRequestPipeData(
  socket: net.Socket,
  pending: Map<string, PendingResolver>,
): void {
  let buffer = Buffer.alloc(0);

  socket.on("data", (chunk: Buffer) => {
    buffer = Buffer.concat([buffer, chunk]);

    while (buffer.length >= 4) {
      const len = buffer.readUInt32BE(0);
      if (buffer.length < 4 + len) break;

      const msgBuf = buffer.subarray(4, 4 + len);
      buffer = buffer.subarray(4 + len);

      try {
        const response = IpcResponse.decode(msgBuf);
        const plain = IpcResponse.toObject(response, {
          longs: String,
          enums: String,
          bytes: Buffer,
        }) as IpcResponseMessage<any>;

        console.log(
          "[BackendHandler] Received:",
          plain.action,
          plain.correlationId,
        );

        // Resolve the matching pending invoke
        const resolver = pending.get(plain.correlationId);
        if (!resolver) continue;

        clearTimeout(resolver.timeoutId);
        pending.delete(plain.correlationId);

        if (plain.error) {
          resolver.reject(new Error(plain.error));
          continue;
        }

        let payload = plain.payload;
        if (Buffer.isBuffer(payload)) {
          try {
            payload = JSON.parse(payload.toString("utf-8"));
          } catch {
            // keep raw Buffer if not JSON
          }
        }

        resolver.resolve(payload);
      } catch (err) {
        console.error("[BackendHandler] Decode error:", err);
      }
    }
  });
}
