import { backendApiService } from "@/services/backend-api-service";
import { Button } from "primereact/button";
import { useState } from "react";

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
      const fakeDto = {
        name: "Test Flow AAA",
        orderNumber: 42,
        flowSearchAreas: [],
      };
      const reply = await backendApiService.Flow.create(fakeDto);
      console.log("Direct reply (if sync):", reply);
      setReply(reply.newId.toString() ?? "skkatoules");
    } catch (err) {
      console.error("Invoke failed:", err);
    }
  };

  return (
    <div className="m-4">
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
