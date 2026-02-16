import { app, BrowserWindow, dialog, ipcMain } from "electron";
import {
  ChildProcess,
  ChildProcessWithoutNullStreams,
  execFile,
} from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";

const { autoUpdater } = pkg;
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

let backendProcess: ChildProcess | null = null;
// let dotnetProcess: ChildProcessWithoutNullStreams | null = null;
let mainWindow: BrowserWindow | null = null;
const isDev = process.env.NODE_ENV === "development" || !app.isPackaged;

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

// Spawn .NET console app
function spawnDotNetProcess() {
  const backendPath = isDev
    ? "dotnet run"
    : path.join(process.resourcesPath, "backend/App.exe");
  const args = isDev
    ? ["run", "--project", path.join(__dirname, "../backend/App/App.csproj")]
    : [];

  backendProcess = execFile(backendPath, (error, stdout, stderr) => {
    if (error) {
      console.error("Failed to run .exe:", error);
      return;
    }
    console.log(" .exe output:", stdout);
  });

  backendProcess.stdout?.on("data", (data) => {
    console.log(`.NET stdout: ${data}`);
    // Parse and handle responses here if needed (e.g., relay back via IPC)
  });

  backendProcess.stderr?.on("data", (data) => {
    console.error(`.NET stderr: ${data}`);
  });

  backendProcess?.on("close", (code) => {
    console.log(`.NET process exited with code ${code}`);
  });

  return backendProcess;
}

// show dialog on update
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
  // autoUpdater.checkForUpdatesAndNotify();
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev to avoid errors
  spawnDotNetProcess();
  // registerHandlers(ipcMain, backendProcess!);
  createWindow();
  // Backend spawn
  // const backendPath = isDev
  //   ? "dotnet"
  //   : path.join(process.resourcesPath, "backend/App.exe");
  // const args = isDev
  //   ? ["run", "--project", path.join(__dirname, "../backend/App/App.csproj")]
  //   : [];

  // backendProcess = execFile(backendPath, (error, stdout, stderr) => {
  //   if (error) {
  //     console.error("Failed to run .exe:", error);
  //     return;
  //   }
  //   console.log(" .exe output:", stdout);
  // });

  // // IPC from React to backend
  // backendProcess.stdout?.on("data", (data) => {
  //   data
  //     .toString()
  //     .trim()
  //     .split("\n")
  //     .forEach((line: any) => {
  //       try {
  //         const msg = JSON.parse(line);
  //         mainWindow?.webContents.send("backend-message", msg); // Push to React
  //       } catch {}
  //     });
  // });

  // backendProcess.stderr?.on("data", (data) =>
  //   console.error("Backend error:", data.toString()),
  // );
  // backendProcess.on("close", () => console.log("Backend closed"));

  ipcMain.handle("send-to-backend", (_, msg: object) => {
    backendProcess?.stdin?.write(JSON.stringify(msg) + "\n");
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
