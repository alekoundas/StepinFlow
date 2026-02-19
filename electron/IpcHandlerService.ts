import { BrowserWindow } from "electron";
import { execFile } from "child_process";
import path from "path";
import log from "electron-log";
import net from "net";
import { fileURLToPath } from "url";

// Type for messages (best practice: Shared types)
// interface Message {
//   action: string;
//   payload: any;
// }

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function IpcHandlerService() {
  // Private method to handle incoming data from .NET, parse it, and send to renderer
  const handleRecivedData = (
    browserWindow: BrowserWindow | null,
    data: any,
  ) => {
    const lines = data.toString().trim().split("\n");
    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      // If it starts with '{' its data.
      // Else its Logging...
      if (trimmed.startsWith("{")) {
        try {
          const msg = JSON.parse(trimmed);
          browserWindow?.webContents.send("recieve-from-backend", msg);
          log.log("Parsed .NET message:", msg);
          console.log("Parsed .NET message:", msg);
        } catch (e) {
          log.error("Failed to parse JSON:", e, "Raw:", trimmed);
          console.error("Failed to parse JSON:", e, "Raw:", trimmed);
        }
      } else {
        log.log("[.NET] " + trimmed);
        console.log("[.NET] " + trimmed);
      }
    }
  };

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

    backendProcess.stdout?.on("data", (data) => {
      handleRecivedData(browserWindow, data);
    });

    backendProcess.stderr?.on("data", (data) => {
      console.error(`[ELECTRON] stderr: ${data}`);
    });

    backendProcess?.on("close", (code) => {
      console.log(`[ELECTRON] process exited with code ${code}`);
    });

    backendProcess.on("spawn", () => {
      console.log("[ELECTRON] process spawned successfully");
    });

    return backendProcess;
  };

  // Debug: Connects to an already running .NET process via named pipe (for development without auto-spawn)
  const connectToDotNetPipe = (browserWindow: BrowserWindow | null) => {
    const pipeName = "\\\\.\\pipe\\stepinflow-backend-pipe"; // Windows named pipe path

    let backendClient: net.Socket | null = net.connect(pipeName, () => {
      log.log("[ELECTRON] Connected to .NET named pipe");
      console.log("[ELECTRON] Connected to .NET named pipe");
    });

    // Stream data for real-time JSON parsing and relay to renderer
    backendClient.on("data", (data) => {
      handleRecivedData(browserWindow, data);
    });

    backendClient.on("error", (err) => {
      log.error("[ELECTRON] Named pipe connection error:", err);
      console.error("[ELECTRON]   Named pipe connection error:", err);
    });

    backendClient.on("close", () => {
      log.log("[ELECTRON] .NET pipe closed");
      console.log("[ELECTRON] .NET pipe closed");
    });

    return backendClient;
  };

  return { spawnDotNetProcess, connectToDotNetPipe };
}

// import { IpcMain, IpcMainInvokeEvent } from "electron";
// import { ChildProcess, ChildProcessWithoutNullStreams } from "child_process";

// // Type for messages (best practice: Shared types)
// interface Message {
//   action: string;
//   payload: any;
// }

// export function registerHandlers(
//   ipcMain: IpcMain,
//   // dotnetProcess: ChildProcessWithoutNullStreams,
//   dotnetProcess: ChildProcess,
// ) {
//   ipcMain.handle(
//     "sendMessageToDotNet",
//     async (event: IpcMainInvokeEvent, message: Message) => {
//       return new Promise((resolve, reject) => {
//         if (!dotnetProcess || dotnetProcess.killed) {
//           return reject(new Error(".NET process not running"));
//         }

//         // Send JSON to .NET stdin
//         dotnetProcess.stdin.write(JSON.stringify(message) + "\n");

//         // Listen for response (assume one-line JSON response for simplicity)
//         const onData = (data: Buffer) => {
//           try {
//             const response = JSON.parse(data.toString().trim());
//             dotnetProcess.stdout.removeListener("data", onData); // Clean up
//             resolve(response);
//           } catch (err) {
//             reject(err);
//           }
//         };

//         dotnetProcess.stdout.once("data", onData);

//         // Timeout for safety
//         setTimeout(() => {
//           dotnetProcess.stdout.removeListener("data", onData);
//           reject(new Error("Response timeout"));
//         }, 5000);
//       });
//     },
//   );
// }
