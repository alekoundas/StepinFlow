import path from "path";
import { fileURLToPath } from "url";
import { app, BrowserWindow, dialog } from "electron";
import { autoUpdater } from "electron-updater";

// __dirname is a special global variable available only in Node.js modules.
//  It contains the directory name of the current module (i.e., the directory where the current JavaScript file is located).
//  In ES modules, __dirname is not defined by default, so we need to create it using the fileURLToPath function from the 'url' module to convert the module's URL to a file path, and then use path.dirname to get the directory name.
const __dirname = path.dirname(fileURLToPath(import.meta.url));

function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"), // optional for now — create empty preload.js if you use it
      nodeIntegration: false, // Recommended: keep false for security
      contextIsolation: true, // Recommended: keep true
      sandbox: true,
    },
  });

  // In development: load Vite dev server
  // In production: load the built index.html
  const isDev = process.env.NODE_ENV === "development" || !app.isPackaged;
  if (isDev) {
    mainWindow.loadURL("http://localhost:5173"); // default Vite port — change if yours is different
    mainWindow.webContents.openDevTools(); // optional: auto-open dev tools
  } else {
    mainWindow.loadFile(path.join(__dirname, "../dist/frontend/index.html")); // adjust path based on your structure
  }

  mainWindow.on("closed", () => {
    // mainWindow = null;
  });
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
  autoUpdater.checkForUpdatesAndNotify();
  createWindow();

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
