import { BrowserWindow } from "electron";
import { ChildProcess, execFile } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { fileURLToPath } from "url";
import Stream from "stream";

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

    handleRecivedData(browserWindow, backendProcess.stdout);

    backendProcess.on("spawn", () => {
      console.log("[ELECTRON] process spawned successfully");
    });

    backendProcess?.on("close", (code) => {
      console.log(`[ELECTRON] process exited with code ${code}`);
      browserWindow?.webContents.send("backend-disconnected");
    });
    return backendProcess;
  };

  // Connects to an already running .NET process via named pipe
  const connectToDotNetPipe = (browserWindow: BrowserWindow | null) => {
    const pipeName = "\\\\.\\pipe\\stepinflow-backend-pipe"; // Windows named pipe path

    const backendPipe: net.Socket = net.connect(pipeName, () => {
      log.log("[ELECTRON] Connected to .NET named pipe");
      console.log("[ELECTRON] Connected to .NET named pipe");
    });

    // Stream data for real-time JSON parsing and relay to renderer
    handleRecivedData(browserWindow, backendPipe);

    backendPipe.on("error", (err) => {
      log.error("[ELECTRON] Named pipe connection error:", err);
      console.error("[ELECTRON]   Named pipe connection error:", err);
    });

    backendPipe.on("close", () => {
      log.log("[ELECTRON] .NET pipe closed");
      console.log("[ELECTRON] .NET pipe closed");
    });

    return backendPipe;
  };

  // Private method to handle incoming data from .NET, parse it, and send to renderer
  const handleRecivedData = (
    browserWindow: BrowserWindow | null,
    backendPipe: net.Socket | Stream.Readable | null,
  ) => {
    let buffer = Buffer.alloc(0);

    backendPipe?.on("data", (chunk: Buffer) => {
      buffer = Buffer.concat([buffer, chunk]);

      // For now — keep line-based JSON (easy migration)
      // Later replace with length-prefix + protobuf parsing
      const lines = buffer.toString("utf-8").split("\n");
      buffer = Buffer.from(lines.pop() || ""); // leftover incomplete line

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed) continue;

        if (trimmed.startsWith("{")) {
          try {
            const msg = JSON.parse(trimmed);
            browserWindow?.webContents.send("recieve-from-backend", msg);
            log.info("[BackendConnector] Parsed message:", msg);
          } catch (err) {
            log.error("[BackendConnector] JSON parse failed:", err, trimmed);
          }
        } else {
          // log line from .NET
          log.info("[.NET]", trimmed);
        }
      }
    });
  };

  return { spawnDotNetProcess, connectToDotNetPipe };
}
