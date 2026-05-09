import { BrowserWindow } from "electron";
import { execFile } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PIPE_NAMES = {
  request: "\\\\.\\pipe\\stepinflow-request",
  broadcast: "\\\\.\\pipe\\stepinflow-broadcast",
};

const MAX_ATTEMPTS = 100;
const RETRY_DELAY_MS = 300;
const CONNECT_TIMEOUT_MS = 2000;

export function BackendService() {
  // ========================================================================
  // (Production only) Spawns the .NET process
  // ========================================================================
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

    backendProcess.on("spawn", () => {
      console.log("[ELECTRON] process spawned successfully");
    });

    backendProcess?.on("close", (code) => {
      console.log(`[ELECTRON] process exited with code ${code}`);
      browserWindow?.webContents.send(IPC_CHANNELS.BACKEND_DISCONNECTED);
    });

    // backendProcess.on("error", (err) => {
    //   log.error("[ELECTRON] Backend process error:", err);
    //   console.error("[ELECTRON] Backend process error:", err);
    // });
    return backendProcess;
  };
  // ========================================================================
  // Request pipe  (Electron -> .NET -> Electron, correlated)
  // ========================================================================
  const connectToRequestPipe = (
    browserWindow: BrowserWindow | null,
    setClient: (s: net.Socket | null) => void,
    onConnected: (s: net.Socket) => void,
  ): Promise<net.Socket> =>
    connectWithRetry(
      PIPE_NAMES.request,
      "request",
      browserWindow,
      setClient,
      onConnected,
    );

  // ========================================================================
  // Broadcast pipe  (.NET -> Electron only, no replies)
  // ========================================================================
  const connectToBroadcastPipe = (
    browserWindow: BrowserWindow | null,
    setClient: (s: net.Socket | null) => void,
    onConnected: (s: net.Socket) => void,
  ): Promise<net.Socket> =>
    connectWithRetry(
      PIPE_NAMES.broadcast,
      "broadcast",
      browserWindow,
      setClient,
      onConnected,
    );

  return { spawnDotNetProcess, connectToRequestPipe, connectToBroadcastPipe };
}

// ========================================================================
// Private methods
// ========================================================================
async function connectWithRetry(
  pipeName: string,
  label: string,
  browserWindow: BrowserWindow | null,
  setClient: (s: net.Socket | null) => void,
  onConnected: (s: net.Socket) => void,
): Promise<net.Socket> {
  for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
    try {
      const socket = await attemptConnect(pipeName);
      console.log(`[ELECTRON][${label}] Connected on attempt ${attempt}`);
      setClient(socket);
      onConnected(socket);
      setupReconnect(
        socket,
        label,
        browserWindow,
        setClient,
        onConnected,
        pipeName,
      );
      return socket;
    } catch (err: any) {
      console.log(
        `[ELECTRON][${label}] Attempt ${attempt} failed: ${err.message}. Retrying in ${RETRY_DELAY_MS}ms…`,
      );

      // Delay before next attempt
      await new Promise((r) => setTimeout(r, RETRY_DELAY_MS));
    }
  }
  throw new Error(
    `[ELECTRON][${label}] Failed to connect after ${MAX_ATTEMPTS} attempts`,
  );
}

function attemptConnect(pipeName: string): Promise<net.Socket> {
  return new Promise((resolve, reject) => {
    const socket = net.connect(pipeName);
    const timeout = setTimeout(() => {
      socket.destroy();
      reject(new Error("Connection timeout"));
    }, CONNECT_TIMEOUT_MS);

    socket.on("connect", () => {
      clearTimeout(timeout);
      resolve(socket);
    });
    socket.once("error", (err) => {
      clearTimeout(timeout);
      reject(err);
    });
  });
}

function setupReconnect(
  client: net.Socket,
  label: string,
  browserWindow: BrowserWindow | null,
  setClient: (s: net.Socket | null) => void,
  onConnected: (s: net.Socket) => void,
  pipeName: string,
) {
  client.on("error", (err) =>
    log.error(`[ELECTRON][${label}] Pipe error:`, err),
  );
  client.on("close", () => {
    log.log(`[ELECTRON][${label}] Pipe closed — reconnecting…`);
    setClient(null);
    connectWithRetry(
      pipeName,
      label,
      browserWindow,
      setClient,
      onConnected,
    ).catch((err) => log.error(`[ELECTRON][${label}] Reconnect failed:`, err));
  });
}
