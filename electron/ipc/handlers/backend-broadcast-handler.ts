import { BrowserWindow } from "electron";
import net from "net";
import log from "electron-log";
import { ProtobufService } from "../../protobuf/protobuf.js";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { BackendService } from "../services/backend-service.js";

const { IpcBroadcast } = await ProtobufService().getMessageTypes();

export async function registerBroadcastHandler(
  mainWindow: BrowserWindow | null,
): Promise<void> {
  let broadcastClient: net.Socket | null = null;

  const setClient = (socket: net.Socket | null) => (broadcastClient = socket);
  const onConnected = (socket: net.Socket) =>
    handleBroadcastPipeData(socket, mainWindow);

  // Connect to the dedicated BROADCAST pipe
  await BackendService().connectToBroadcastPipe(
    mainWindow,
    setClient,
    onConnected,
  );
}

// ── Reads IpcBroadcast messages and forwards them to the renderer ──
function handleBroadcastPipeData(
  socket: net.Socket,
  mainWindow: BrowserWindow | null,
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
        const broadcast = IpcBroadcast.decode(msgBuf);
        const plain = IpcBroadcast.toObject(broadcast, { bytes: Buffer });

        console.log("[BroadcastHandler] Received:", plain.type ?? plain.action);

        // Broadcast on all windows
        BrowserWindow.getAllWindows().forEach((win) => {
          if (!win.isDestroyed()) {
            win.webContents.send(IPC_CHANNELS.BACKEND_BROADCAST, {
              type: plain.type ?? plain.action,
              payload: JSON.parse(Buffer.from(plain.payload).toString("utf-8")),
            });
          }
        });
        // mainWindow?.webContents.send(IPC_CHANNELS.BACKEND_BROADCAST, {
        //   type: plain.type ?? plain.action,
        //   payload: JSON.parse(Buffer.from(plain.payload).toString("utf-8")),
        // });
      } catch (err) {
        log.error("[BroadcastHandler] Decode error:", err);
      }
    }
  });

  socket.on("error", (err) =>
    log.error("[BroadcastHandler] Socket error:", err),
  );
}
