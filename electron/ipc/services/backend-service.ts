import { BrowserWindow } from "electron";
import { spawn, type ChildProcess } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { BackendStateEnum } from "../../shared/types.js";
import { IS_DEVELOPING, OUT_DIR } from "../../app-paths.js";
import { getBackendStatus, setBackendStopped } from "./backend-status-service.js";

const PIPE_NAMES = {
  request: "\\\\.\\pipe\\stepinflow-request",
  broadcast: "\\\\.\\pipe\\stepinflow-broadcast",
};

// A request that finds no connection fails after about 30 seconds rather than hanging.
export const ON_DEMAND_CONNECT_ATTEMPTS = 100;
const RETRY_DELAY_MS = 300;
const CONNECT_TIMEOUT_MS = 2000;
const ATTEMPTS_PER_LOG_LINE = 100;

// The end of what the backend wrote to stderr, which is where an exception that ends it goes.
const MAX_STOP_REASON_LENGTH = 8000;

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
  const spawnDotNetProcess = (): ChildProcess => {
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

    let errors = "";

    backendProcess.stdout?.on("data", (chunk: Buffer) => log.info(`[BACKEND] ${chunk.toString().trimEnd()}`));
    backendProcess.stderr?.on("data", (chunk: Buffer) => {
      const text = chunk.toString().trimEnd();
      log.error(`[BACKEND] ${text}`);
      errors = `${errors}\n${text}`.slice(-MAX_STOP_REASON_LENGTH);
    });

    backendProcess.on("spawn", () => {
      log.info("[ELECTRON] Backend process started");
    });

    backendProcess.on("error", (error) => {
      log.error("[ELECTRON] Backend process could not start:", error);
      setBackendStopped(error.message);
    });

    backendProcess.on("close", (code) => {
      log.info(`[ELECTRON] Backend process exited with code ${code}`);
      setBackendStopped(errors.trim() || `The backend exited with code ${code}.`);
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
    maxAttempts?: number,
  ): Promise<net.Socket> =>
    connectWithRetry(
      PIPE_NAMES.request,
      "request",
      browserWindow,
      setClient,
      onConnected,
      maxAttempts,
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
  maxAttempts: number = Number.POSITIVE_INFINITY,
): Promise<net.Socket> {
  // With no limit it tries for as long as the backend might still come up: the pipes wait for the
  // migrations, however long those take, and a backend in Visual Studio can be started at any time.
  for (
    let attempt = 1;
    attempt <= maxAttempts && getBackendStatus().state !== BackendStateEnum.STOPPED;
    attempt++
  ) {
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
      if (attempt % ATTEMPTS_PER_LOG_LINE === 1)
        console.log(
          `[ELECTRON][${label}] Attempt ${attempt} failed: ${err.message}. Retrying every ${RETRY_DELAY_MS}ms…`,
        );

      // Delay before next attempt
      await new Promise((r) => setTimeout(r, RETRY_DELAY_MS));
    }
  }
  throw new Error(`[ELECTRON][${label}] Gave up connecting to the backend`);
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
