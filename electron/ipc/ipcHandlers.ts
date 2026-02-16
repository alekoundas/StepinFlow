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
