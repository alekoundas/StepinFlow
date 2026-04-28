import protobuf from "protobufjs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function ProtobufService() {
  const getProtobufRoot = async () => {
    const protoPath = path.join(__dirname, "./protobuf.proto");

    const root = new protobuf.Root();
    await root.load(protoPath, {
      keepCase: false, // preserve field names as-is
      alternateCommentMode: true,
    });

    return root;
  };

  const getMessageTypes = async () => {
    const root = await getProtobufRoot();
    return {
      IpcRequest: root.lookupType("IpcRequest"),
      IpcResponse: root.lookupType("IpcResponse"),
      IpcBroadcast: root.lookupType("IpcBroadcast"),
    };
  };

  return { getMessageTypes };
}
