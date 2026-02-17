import { Button } from "primereact/button";
import { useEffect, useState } from "react";

export default function HomePage() {
  const [logs, setLogs] = useState<string[]>([]);
  const [status, setStatus] = useState("Ready");

  useEffect(() => {
    // Register listener ONCE when component mounts
    const api = (window as any).backendApi;

    if (!api) {
      console.error("backendApi not exposed");
      setStatus("IPC not available");
      return;
    }

    api.onMessage((msg: any) => {
      console.log("Received from .NET:", msg);
      setLogs((prev) => [...prev, JSON.stringify(msg)]);

      // Example: handle progress messages if you later send them
      if (msg.event === "progress") {
        setLogs((prev) => [...prev, `Progress: ${msg.details}`]);
      }
    });
  }, []);

  const onSend = async () => {
    const api = (window as any).backendApi;

    if (!api) {
      setStatus("Cannot send – IPC not ready");
      return;
    }

    setStatus("Sending...");

    try {
      // Send a real message object
      await api.send({
        action: "greet",
        payload: "Hello from React!",
      });

      setStatus("Message sent!");
      console.log("Sent to backend:", {
        action: "greet",
        payload: "Hello from React!",
      });
    } catch (err) {
      console.error("Send failed:", err);
      setStatus("Send failed");
    }
  };

  return (
    <div className="p-4">
      <h2>StepInFlow Test</h2>
      <p>Status: {status}</p>

      <Button
        label="Send Greeting to .NET"
        icon="pi pi-send"
        onClick={onSend}
        className="p-button-success mb-4"
      />

      <div>
        <h3>Received messages:</h3>
        {logs.length === 0 ? (
          <p className="text-gray-500">Nothing received yet...</p>
        ) : (
          <ul className="list-disc pl-5">
            {logs.map((log, i) => (
              <li key={i}>{log}</li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
