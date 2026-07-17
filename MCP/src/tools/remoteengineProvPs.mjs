import { execFile } from "node:child_process";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

export const remoteengineProvPsTool = {
  name: "remoteengine_list_providers",
  description:
    "remoteengine で起動中のプロバイダー一覧を取得するツール。ユーザーが『remoteengineで起動中のプロバイダーを教えて』『プロバイダー一覧を確認して』と依頼したときに使う。",
  schema: {},
  handler: async () => {
    try {
      const { stdout, stderr } = await execFileAsync("orin3.remoteengine", ["prov", "ps"]);

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                status: "ok",
                command: "orin3.remoteengine prov ps",
                stdout,
                stderr,
              },
              null,
              2
            ),
          },
        ],
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                status: "error",
                command: "orin3.remoteengine prov ps",
                message: error instanceof Error ? error.message : String(error),
              },
              null,
              2
            ),
          },
        ],
      };
    }
  },
};
