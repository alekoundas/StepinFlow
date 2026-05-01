import { BrowserWindow, ipcMain, Net } from "electron";
import net from "net";
import log from "electron-log";
import { ProtobufService } from "../protobuf/protobuf.js";
import { IPC_CHANNELS } from "../shared/channels.js";
import type { IpcRequestMessage, IpcResponseMessage } from "../shared/types.js";
import { BackendService } from "../backend-service.js";

interface PendingResolver {
  resolve: (value: any) => void;
  reject: (reason: any) => void;
  timeoutId: ReturnType<typeof setTimeout>;
}
const { IpcRequest, IpcResponse, IpcBroadcast } =
  await ProtobufService().getMessageTypes();
export type InvokeBackend = (action: string, payload: unknown) => Promise<any>;

export async function registerBackendHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): Promise<{
  invokeBackend: InvokeBackend;
}> {
  const pending = new Map<string, PendingResolver>();
  const backendService = BackendService(); // Reuse same instance.

  // ===================================================================
  // Connect Request Pipe (for invoke + responses)
  // ===================================================================
  const requestBackendClient = await backendService.connectToRequestPipe(
    (socket) => setupRequestPipeListener(socket, mainWindow, pending),
  );

  // ===================================================================
  //  Connect Broadcast Pipe (for mouse events, execution updates, etc.)
  // ===================================================================
  await backendService.connectToBroadcastPipe((msgBuf: Buffer) =>
    setupBroadcastPipeListener(msgBuf, mainWindow),
  );

  // ===================================================================
  // IPC handle: Renderer -> Electron -> . (Request Pipe)
  // ===================================================================
  ipcMain.handle(
    IPC_CHANNELS.BACKEND_SEND,
    async (_, msg: IpcRequestMessage) => {
      console.log("[BackendHandler] Sending to backend:", msg);
      return invokeBackend(msg.action, msg.payload);
    },
  );

  // ===================================================================
  // Expose reusable invoke .Net method (also used on other IpcHandlers in electron)
  // ===================================================================
  const invokeBackend: InvokeBackend = async (
    action: string,
    payload: unknown,
  ): Promise<any> => {
    let client = backendService.getRequestClient();

    // If client is not writable, try reconnecting.
    if (!client?.writable) {
      log.log("[BackendHandler]: Pipe not connected — reconnecting...");
      client = await BackendService().connectToRequestPipe((socket) =>
        setupRequestPipeListener(socket, mainWindow, pending),
      );
      client = requestBackendClient;
    }

    const correlationId = crypto.randomUUID();
    const payloadBytes = Buffer.from(JSON.stringify(payload));
    const reqObj = { action, payload: payloadBytes, correlationId };

    // Verify protobuf
    const verifyErr = IpcRequest.verify(reqObj);
    if (verifyErr) throw new Error(verifyErr);

    const encoded = IpcRequest.encode(IpcRequest.create(reqObj)).finish();
    const prefix = Buffer.alloc(4);
    prefix.writeUInt32BE(encoded.length, 0);
    client.write(Buffer.concat([prefix, encoded]));

    return new Promise<any>((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        pending.delete(correlationId);
        reject(new Error(`[BackendHandler] Timeout for "${action}"`));
      }, 10_000); // 10 second timeout

      pending.set(correlationId, { resolve, reject, timeoutId });
    });
  };

  return { invokeBackend };
}

// ===================================================================
// Listeners for .Net responses and broadcasts
// ===================================================================

// Listens and handles IpcResponse messages from .Net
function setupRequestPipeListener(
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
            } catch {}
          }

          resolver.resolve(payload);
        }
      } catch (err) {
        console.error("[BackendHandler] Decode error:", err);
      }
    }
  });
}

// Listens and handles IpcBroadcast messages from .Net
function setupBroadcastPipeListener(
  msgBuf: Buffer,
  mainWindow: BrowserWindow | null,
) {
  try {
    const broadcast = IpcBroadcast.decode(msgBuf);
    const plain = IpcBroadcast.toObject(broadcast);

    mainWindow?.webContents.send(IPC_CHANNELS.BACKEND_BROADCAST, {
      type: plain.type,
      payload: JSON.parse(Buffer.from(plain.payload as Uint8Array).toString()),
    });
  } catch (err) {
    console.error("[Broadcast Pipe] Decode error:", err);
  }
}
