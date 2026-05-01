import { BrowserWindow } from "electron";
import { execFile } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "./shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function BackendService() {
  let requestClient: net.Socket | null = null;

  //==============================================================
  // Public methods
  //==============================================================

  const getRequestClient = () => requestClient;

  // Spawns the .NET process and sets up listeners for its output
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

  const connectToRequestPipe = async (
    onConnected: (socket: net.Socket) => void,
  ): Promise<net.Socket> => {
    return connectToDotNetPipe("stepinflow-request", (client) => {
      requestClient = client;
      onConnected(client);
    });
  };

  const connectToBroadcastPipe = async (
    onData: (data: Buffer) => void,
  ): Promise<net.Socket> => {
    return connectToDotNetPipe("stepinflow-broadcast", (client) => {
      let buffer = Buffer.alloc(0);
      client.on("data", (chunk: Buffer) => {
        buffer = Buffer.concat([buffer, chunk]);

        while (buffer.length >= 4) {
          const len = buffer.readUInt32BE(0);
          if (buffer.length < 4 + len) break;

          const msgBuf = buffer.subarray(4, 4 + len);
          buffer = buffer.subarray(4 + len);

          onData(msgBuf);
        }
      });
    });
  };

  //==============================================================
  // Private methods
  //==============================================================

  // Connects to .NET process via named pipe, with retry and auto-reconnect
  const connectToDotNetPipe = async (
    pipeName: string,
    onConnected: (socket: net.Socket) => void,
  ): Promise<net.Socket> => {
    const fullPipeName = "\\\\.\\pipe\\" + pipeName;
    const MAX_ATTEMPTS = 999;
    const RETRY_DELAY_MS = 300;

    for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
      try {
        const client = await attemptConnect(fullPipeName);
        console.log(
          `[ELECTRON]: Connected to .NET named pipe "${pipeName}" on attempt ${attempt}`,
        );
        onConnected(client);
        setupReconnect(client, pipeName, onConnected);

        return client;
      } catch (err: any) {
        console.log(
          `[ELECTRON]: ${pipeName} -> Connect attempt ${attempt} failed: ${err.message}. Retrying in ${RETRY_DELAY_MS}ms...`,
        );
        await new Promise((r) => setTimeout(r, RETRY_DELAY_MS));
      }
    }
    throw new Error(
      `[ELECTRON]: Failed to connect to .NET pipe "${pipeName}" after ${MAX_ATTEMPTS} attempts`,
    );
  };

  // Try to connect to the pipe
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

  // Reconect to .NET pipe on disconnect
  const setupReconnect = (
    client: net.Socket,
    pipeName: string,
    onConnected: (socket: net.Socket) => void,
  ) => {
    client.on("error", (err) => {
      log.error("[ELECTRON]: Named pipe connection error:", err);
      console.error("[ELECTRON]: Named pipe connection error:", err);
    });

    client.on("close", () => {
      log.log("[ELECTRON]: .NET pipe closed, starting reconnect...");
      console.log("[ELECTRON]: .NET pipe closed, starting reconnect...");
      requestClient = null;

      // background reconnect (auto-retry built-in)
      connectToDotNetPipe(pipeName, onConnected).catch((err) => {
        log.error("[ELECTRON]: Reconnect failed:", err);
        console.error("[ELECTRON]: Reconnect failed:", err);
      });
    });
  };

  return {
    spawnDotNetProcess,
    connectToRequestPipe,
    connectToBroadcastPipe,
    getRequestClient,
  };
}
