import { Button } from "primereact/button";

export default function HomePage() {
  const onSend = async () => {
    // const res = await window..sendMessageToDotNet(message);v
    (window as any).backendApi?.send((msg: any) => {
      if (msg.event === "progress") {
        // setLog(prev => [...prev, msg.details]);
      }
    });

    (window as any).backendApi.onMessage((msg: any) => {
      if (msg.event === "progress") {
        // setLog(prev => [...prev, msg.details]);
      }
    });
  };

  return (
    <div className="flex flex-row">
      <h1>sssssssssss</h1>
      <Button
        label="Send"
        icon="pi pi-check"
        onClick={onSend}
        autoFocus
      />
    </div>
  );
}
