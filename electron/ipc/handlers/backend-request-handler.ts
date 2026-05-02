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
const { IpcRequest, IpcResponse, IpcBroadcast } =
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
    handleRequestPipeData(socket, mainWindow, pending);

  //============================================
  // Initial connection
  //============================================
  backendClient = await BackendService().connectToRequestPipe(
    mainWindow,
    setClient,
    onConnected,
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

// Handles IpcResponse or IpcBroadcast messages from .Net
function handleRequestPipeData(
  socket: net.Socket,
  mainWindow: BrowserWindow | null,
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

      // Try decoding as IpcResponse first
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

        // Push to renderer for unsolicited events (execution progress, etc.)
        // mainWindow?.webContents.send(IPC_CHANNELS.BACKEND_RECEIVE, plain);

        // Resolve the matching pending invoke
        const resolver = pending.get(plain.correlationId);
        if (resolver) {
          clearTimeout(resolver.timeoutId);
          pending.delete(plain.correlationId);

          if (plain.error) {
            resolver.reject(new Error(plain.error));
            return;
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
        }
      } catch (err) {
        console.error("[BackendHandler] Decode error:", err);
      }

      // If not an IpcResponse, try decoding as IpcBroadcast
      try {
        const ipcBroadcast = IpcBroadcast.decode(msgBuf);
        const plain = IpcBroadcast.toObject(ipcBroadcast);
        // const payload = JSON.parse(plain.payload.toString("utf-8"));

        mainWindow?.webContents.send(IPC_CHANNELS.BACKEND_BROADCAST, {
          action: plain.action,
          payload: JSON.parse(Buffer.from(plain.payload).toString()),
        });
        return; // important: don't treat as normal response
      } catch {}
    }
  });
}
