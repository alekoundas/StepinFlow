import { app, BrowserWindow, dialog, ipcMain } from "electron";
import { execFile } from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";
import net from "net";

const { autoUpdater } = pkg;
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// C# DEBUG
let backendClient: net.Socket | null = null; // For named pipe connection
const pipeName = "\\\\.\\pipe\\stepinflow-backend-pipe"; // Windows named pipe path (cross-platform compatible)

let backendProcess: ReturnType<typeof execFile> | null = null;
let mainWindow: BrowserWindow | null = null;
const isDev = process.env.NODE_ENV === "development" || !app.isPackaged;

// Spawn .NET console app (conditionally skipped if NO_API=1)
function spawnDotNetProcess() {
  const backendPath = isDev
    ? "dotnet"
    : path.join(process.resourcesPath, "backend/App.exe");
  const args = isDev
    ? ["run", "--project", path.join(__dirname, "../../backend/App/App.csproj")]
    : [];

  // Use execFile with args and optional callback (fires on exit)
  backendProcess = execFile(backendPath, args, (error, stdout, stderr) => {
    if (error) {
      console.error("Failed to run .NET process:", error);
      return;
    }
    console.log(".NET buffered output (on exit):", stdout);
    if (stderr) console.error(".NET buffered stderr (on exit):", stderr);
  });

  // Stream stdout for real-time JSON parsing and relay to renderer
  // backendProcess.stdout?.on("data", (data) => {
  //   data
  //     .toString()
  //     .trim()
  //     .split("\n")
  //     .forEach((line: string) => {
  //       try {
  //         const msg = JSON.parse(line);
  //         mainWindow?.webContents.send("backend-message", msg); // Relay to React
  //         console.log("Parsed .NET message:", msg); // Debug
  //       } catch (e) {
  //         console.error(
  //           "Failed to parse backend message:",
  //           e,
  //           "Raw line:",
  //           line,
  //         );
  //       }
  //     });
  // });

  backendProcess.stdout?.on("data", (data) => {
    const lines = data.toString().trim().split("\n");
    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue; // Skip empty

      // Quick check: only try parse if it starts with { (JSON object)
      if (trimmed.startsWith("{")) {
        try {
          const msg = JSON.parse(trimmed);
          mainWindow?.webContents.send("backend-message", msg);
          console.log("Parsed .NET message:", msg);
        } catch (e) {
          console.error("Failed to parse JSON:", e, "Raw:", trimmed);
        }
      } else {
        // Optional: log non-JSON startup/debug lines differently
        console.log("[.NET startup] " + trimmed);
        // Or ignore completely: // do nothing
      }
    }
  });

  backendProcess.stderr?.on("data", (data) => {
    console.error(`.NET stderr: ${data}`);
  });

  backendProcess?.on("close", (code) => {
    console.log(`.NET process exited with code ${code}`);
  });

  return backendProcess;
}

function connectToDotNetPipe() {
  backendClient = net.connect(pipeName, () => {
    console.log("Connected to .NET named pipe");
  });

  // Stream data for real-time JSON parsing and relay to renderer
  backendClient.on("data", (data) => {
    const lines = data.toString().trim().split("\n");
    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      if (trimmed.startsWith("{")) {
        try {
          const msg = JSON.parse(trimmed);
          mainWindow?.webContents.send("backend-message", msg);
          console.log("Parsed .NET message:", msg);
        } catch (e) {
          console.error("Failed to parse JSON:", e, "Raw:", trimmed);
        }
      } else {
        console.log("[.NET startup] " + trimmed);
      }
    }
  });

  backendClient.on("error", (err) => {
    console.error("Named pipe connection error:", err);
  });

  backendClient.on("close", () => {
    console.log(".NET pipe closed");
  });

  return backendClient;
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      preload: path.join(
        __dirname,
        isDev ? "preload.js" : "../dist/preload.js",
      ),
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
    },
  });

  if (isDev) {
    mainWindow.loadURL("http://localhost:5173");
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(path.join(__dirname, "../dist/frontend/index.html"));
    // mainWindow.webContents.openDevTools();
  }

  mainWindow.on("closed", () => {});
}

// Auto-updater dialog
autoUpdater.on("update-downloaded", () => {
  dialog
    .showMessageBox({
      type: "info",
      title: "Update Available",
      message: "A new version is ready. Restart now?",
      buttons: ["Yes", "Later"],
    })
    .then((result) => {
      if (result.response === 0) autoUpdater.quitAndInstall();
    });
});

app.whenReady().then(() => {
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev
  // Conditionally spawn backend (skipped in dev:no-api)
  // if (process.env.NO_API !== "1") {
  // spawnDotNetProcess();
  connectToDotNetPipe();
  // }
  createWindow();

  ipcMain.handle("send-to-backend", (_, msg: object) => {
    if (backendClient) {
      backendClient.write(JSON.stringify(msg) + "\n");
    } else if (backendProcess && backendProcess.stdin) {
      backendProcess.stdin.write(JSON.stringify(msg) + "\n");
    } else {
      console.error("Backend process not available for sending message");
    }
  });

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
