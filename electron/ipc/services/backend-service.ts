import { BrowserWindow } from "electron";
import { spawn, type ChildProcess } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { IS_DEVELOPING, OUT_DIR } from "../../app-paths.js";

const PIPE_NAMES = {
  request: "\\\\.\\pipe\\stepinflow-request",
  broadcast: "\\\\.\\pipe\\stepinflow-broadcast",
};

const MAX_ATTEMPTS = 100;
const RETRY_DELAY_MS = 300;
const CONNECT_TIMEOUT_MS = 2000;

/**
 * How the backend is reached. A release always starts its own App.exe. While developing, it is
 * the one already running in Visual Studio, unless Electron was started with --spawn-backend, which
 * starts it with dotnet run.
 */
export function shouldSpawnBackend(): boolean {
  return !IS_DEVELOPING || process.argv.includes("--spawn-backend");
}

export function BackendService() {
  // ========================================================================
  // Starts the .NET process
  // ========================================================================
  const spawnDotNetProcess = (browserWindow: BrowserWindow | null): ChildProcess => {
    // The backend leaves when this process does, however this one ends.
    const parent = ["--parent-pid", String(process.pid)];

    let backendPath = path.join(process.resourcesPath, "backend/App.exe");
    let args = parent;

    if (IS_DEVELOPING) {
      backendPath = "dotnet";
      args = ["run", "--project", path.join(OUT_DIR, "../../backend/App/App.csproj"), "--", ...parent];
    }

    // Streamed rather than buffered: a buffer has a ceiling, and passing it kills the backend.
    const backendProcess = spawn(backendPath, args, { windowsHide: true });

    backendProcess.stdout?.on("data", (chunk: Buffer) => log.info(`[BACKEND] ${chunk.toString().trimEnd()}`));
    backendProcess.stderr?.on("data", (chunk: Buffer) => log.error(`[BACKEND] ${chunk.toString().trimEnd()}`));

    backendProcess.on("spawn", () => {
      log.info("[ELECTRON] Backend process started");
    });

    backendProcess.on("error", (error) => {
      log.error("[ELECTRON] Backend process could not start:", error);
    });

    backendProcess.on("close", (code) => {
      log.info(`[ELECTRON] Backend process exited with code ${code}`);
      browserWindow?.webContents.send(IPC_CHANNELS.BACKEND_DISCONNECTED);
    });

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
