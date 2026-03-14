import { BrowserWindow } from "electron";
import { execFile } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { fileURLToPath } from "url";
import Stream from "stream";
import { ProtobufService } from "./protobuf/protobuf.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function IpcHandlerService() {
  // Production: Spawns the .NET process and sets up listeners for its output
  const spawnDotNetProcess = (
    browserWindow: BrowserWindow | null,
    isDev: boolean,
  ): ReturnType<typeof execFile> | null => {
    let backendPath: string;
    let args: string[] = [];

    if (isDev) {
      backendPath = "dotnet";
      args = [
        "run",
        "--project",
        path.join(__dirname, "../../backend/App/App.csproj"),
      ];
    } else {
      backendPath = path.join(process.resourcesPath, "backend/App.exe");
    }

    const backendProcess: ReturnType<typeof execFile> | null = execFile(
      backendPath,
      args,
      (error, stdout, stderr) => {
        if (error) {
          log.error("[ELECTRON] Failed to run .NET process:", error);
          console.error("[ELECTRON] Failed to run .NET process:", error);
          return;
        }
        if (stdout) {
          log.log("[ELECTRON] buffered output (on exit):", stdout);
          console.log("[ELECTRON] buffered output (on exit):", stdout);
        }
        if (stderr) {
          log.error("[ELECTRON] buffered stderr (on exit):", stderr);
          console.error("[ELECTRON] buffered stderr (on exit):", stderr);
        }
      },
    );

    // await handleRecivedData(browserWindow, backendProcess.stdout);

    backendProcess.on("spawn", () => {
      console.log("[ELECTRON] process spawned successfully");
    });

    backendProcess?.on("close", (code) => {
      console.log(`[ELECTRON] process exited with code ${code}`);
      browserWindow?.webContents.send("backend-disconnected");
    });

    // backendProcess.on("error", (err) => {
    //   log.error("[ELECTRON] Backend process error:", err);
    //   console.error("[ELECTRON] Backend process error:", err);
    // });
    return backendProcess;
  };

  // ──────────────────────────────────────────────────────────────
  // NEW: Robust connect with retry + auto-reconnect on close/error
  // ──────────────────────────────────────────────────────────────
  const connectToDotNetPipe = async (
    browserWindow: BrowserWindow | null,
    setClient: (client: net.Socket | null) => void,
    pendingResponses: Map<string, (plain: any) => void>,
  ): Promise<net.Socket> => {
    const pipeName = "\\\\.\\pipe\\stepinflow-backend-pipe";
    const MAX_ATTEMPTS = 50; // ~15 seconds max for initial connect
    const RETRY_DELAY_MS = 300;

    for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
      try {
        const client = await attemptConnect(pipeName);
        console.log(
          `[ELECTRON] Connected to .NET named pipe on attempt ${attempt}`,
        );
        setClient(client);

        await handleRecivedData(browserWindow, client, pendingResponses);
        setupReconnect(client, browserWindow, setClient, pendingResponses);

        return client;
      } catch (err: any) {
        console.log(
          `[ELECTRON] Connect attempt ${attempt} failed: ${err.message}. Retrying in ${RETRY_DELAY_MS}ms...`,
        );
        await new Promise((r) => setTimeout(r, RETRY_DELAY_MS));
      }
    }
    throw new Error(
      `[ELECTRON] Failed to connect to .NET pipe after ${MAX_ATTEMPTS} attempts`,
    );
  };

  const attemptConnect = (pipeName: string): Promise<net.Socket> =>
    new Promise((resolve, reject) => {
      const socket = net.connect(pipeName);
      const timeout = setTimeout(() => {
        socket.destroy();
        reject(new Error("Connection timeout"));
      }, 2000);

      socket.on("connect", () => {
        clearTimeout(timeout);
        resolve(socket);
      });

      socket.once("error", (err) => {
        clearTimeout(timeout);
        reject(err);
      });
    });

  const setupReconnect = (
    client: net.Socket,
    browserWindow: BrowserWindow | null,
    setClient: (client: net.Socket | null) => void,
    pendingResponses: Map<string, (plain: any) => void>,
  ) => {
    client.on("error", (err) => {
      log.error("[ELECTRON] Named pipe connection error:", err);
      console.error("[ELECTRON] Named pipe connection error:", err);
    });

    client.on("close", () => {
      log.log("[ELECTRON] .NET pipe closed, starting reconnect...");
      console.log("[ELECTRON] .NET pipe closed, starting reconnect...");
      setClient(null);

      // background reconnect (auto-retry built-in)
      connectToDotNetPipe(browserWindow, setClient, pendingResponses).catch(
        (err) => {
          log.error("[ELECTRON] Reconnect failed:", err);
          console.error("[ELECTRON] Reconnect failed:", err);
        },
      );
    });
  };

  // ──────────────────────────────────────────────────────────────
  // Updated to support pending invoke responses
  // ──────────────────────────────────────────────────────────────
  const handleRecivedData = async (
    browserWindow: BrowserWindow | null,
    backendPipe: net.Socket | Stream.Readable | null,
    pendingResponses: Map<string, (plain: any) => void>,
  ) => {
    let buffer = Buffer.alloc(0);
    const { IpcResponse } = await ProtobufService().getMessageTypes();

    backendPipe?.on("data", (chunk: Buffer) => {
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
          });

          // Always forward to renderer (onMessage still works)
          browserWindow?.webContents.send("recieve-from-backend", plain);
          console.log("[Electron] Received response:", plain);

          // If this is a response to an `invoke`, resolve the promise
          const cid = plain.correlationId;
          if (cid && pendingResponses.has(cid)) {
            const resolver = pendingResponses.get(cid)!;
            pendingResponses.delete(cid);
            resolver(plain);
          }
        } catch (err) {
          console.error("[Electron] Decode error:", err);
        }
      }
    });
  };

  return { spawnDotNetProcess, connectToDotNetPipe };
}
