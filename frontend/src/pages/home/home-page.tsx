import { Button } from "primereact/button";
import { useState } from "react";
import { backendApiService } from "../../services/backend-api-service";
import { Flow } from "../../models/dto/flow";

export default function HomePage() {
  // Subscribe once (better in root layout, but ok here for now)
  // useBackendEvents();

  // const { status, logs } = useBackendStore();
  const [reply, setReply] = useState("Ready");
  // setupResponseListener();
  const onGreet = async () => {
    try {
      const reply = await backendApiService.greet("Electron User");
      console.log("Direct reply (if sync):", reply);
      setReply(reply.greeting ?? "skkatoules");
    } catch (err) {
      console.error("Invoke failed:", err);
    }
  };

  const onGreet2 = async () => {
    try {
      const reply = await backendApiService.Flow.create(new Flow());
      console.log("Direct reply (if sync):", reply);
      setReply(reply.newId.toString() ?? "skkatoules");
    } catch (err) {
      console.error("Invoke failed:", err);
    }
  };

  return (
    <div className="p-4">
      <h2>StepInFlow</h2>
      <p>Status: {status}</p>

      <Button
        label="Greet .NET"
        onClick={onGreet}
        className="mb-4 p-button-success"
      />

      <Button
        label="flow create .NET"
        onClick={onGreet2}
        className="mb-4 p-button-success"
      />

      <h3>Received from .NET:</h3>
      {reply.length === 0 ? (
        <p>No messages yet...</p>
      ) : (
        <ul className="list-disc pl-5">{reply}</ul>
      )}
    </div>
  );
}
