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

  // Connects to an already running .NET process via named pipe
  // const connectToDotNetPipe = async (browserWindow: BrowserWindow | null) => {
  //   const pipeName = "\\\\.\\pipe\\stepinflow-backend-pipe"; // Windows named pipe path

  //   const backendPipe: net.Socket = net.connect(pipeName, () => {
  //     log.log("[ELECTRON] Connected to .NET named pipe");
  //     console.log("[ELECTRON] Connected to .NET named pipe");
  //   });

  //   // Stream data for real-time JSON parsing and relay to renderer
  //   await handleRecivedData(browserWindow, backendPipe);

  //   backendPipe.on("error", async (err) => {
  //     log.error("[ELECTRON] Named pipe connection error:", err);
  //     console.error("[ELECTRON]   Named pipe connection error:", err);
  //     // setTimeout(() => connectToDotNetPipe(browserWindow), 1000);
  //   });

  //   backendPipe.on("close", async () => {
  //     log.log("[ELECTRON] .NET pipe closed");
  //     console.error("[ELECTRON] .NET pipe closed");
  //     // await connectToDotNetPipe(browserWindow);
  //   });

  //   return backendPipe;
  // };

  // // TODO RENAME
  // // Private method to handle incoming data from .NET, parse it, and send to renderer
  // const handleRecivedData = async (
  //   browserWindow: BrowserWindow | null,
  //   backendPipe: net.Socket | Stream.Readable | null,
  // ) => {
  //   let buffer = Buffer.alloc(0);

  //   //   backendPipe?.on("data", (chunk: Buffer) => {
  //   //     buffer = Buffer.concat([buffer, chunk]);

  //   //     // For now — keep line-based JSON (easy migration)
  //   //     // Later replace with length-prefix + protobuf parsing
  //   //     const lines = buffer.toString("utf-8").split("\n");
  //   //     buffer = Buffer.from(lines.pop() || ""); // leftover incomplete line

  //   //     for (const line of lines) {
  //   //       const trimmed = line.trim();
  //   //       if (!trimmed) continue;

  //   //       if (trimmed.startsWith("{")) {
  //   //         try {
  //   //           const msg = JSON.parse(trimmed);
  //   //           browserWindow?.webContents.send("recieve-from-backend", msg);
  //   //           log.info("[BackendConnector] Parsed message:", msg);
  //   //         } catch (err) {
  //   //           log.error("[BackendConnector] JSON parse failed:", err, trimmed);
  //   //         }
  //   //       } else {
  //   //         // log line from .NET
  //   //         log.info("[.NET]", trimmed);
  //   //       }
  //   //     }
  //   //   });
  //   // };
  //   const { IpcResponse } = await ProtobufService().getMessageTypes();
  //   backendPipe?.on("data", (chunk: Buffer) => {
  //     buffer = Buffer.concat([buffer, chunk]);

  //     while (buffer.length >= 4) {
  //       const len = buffer.readUInt32BE(0);
  //       if (buffer.length < 4 + len) break;

  //       const msgBuf = buffer.subarray(4, 4 + len);
  //       buffer = buffer.subarray(4 + len);

  //       try {
  //         const response = IpcResponse.decode(msgBuf);
  //         const plain = IpcResponse.toObject(response, {
  //           longs: String,
  //           enums: String,
  //           bytes: Buffer,
  //         });

  //         browserWindow?.webContents.send("recieve-from-backend", plain);
  //         console.log("[Electron] Received response:", plain);
  //       } catch (err) {
  //         console.error("[Electron] Decode error:", err);
  //       }
  //     }
  //   });
  // };

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
