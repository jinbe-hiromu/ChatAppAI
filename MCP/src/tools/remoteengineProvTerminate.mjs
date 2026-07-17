import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { z } from "zod";

const execFileAsync = promisify(execFile);

function parsePidFromProviderList(stdout) {
  const lines = stdout
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  for (const line of lines) {
    const match = line.match(/\b(\d+)\b/);
    if (match) {
      return match[1];
    }
  }

  return null;
}

export const remoteengineProvTerminateTool = {
  name: "remoteengine_terminate_provider",
  description:
    "remoteengine のプロバイダーを停止するツール。ユーザーが『KVを停止して』『MQTTプロバイダーを停止して』のようにプロバイダー名で停止を依頼したとき、または PID 指定で停止したいときに使う。providerName または pid のどちらかを指定する。",
  schema: {
    providerName: z.string().min(1).optional(),
    pid: z.coerce.number().int().positive().optional(),
  },
  handler: async ({ providerName, pid }) => {
    if (!providerName && !pid) {
      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                status: "error",
                message: "providerName または pid のどちらかを指定してください。",
              },
              null,
              2
            ),
          },
        ],
      };
    }

    try {
      let resolvedPid = pid ? String(pid) : null;
      let lookupCommand = null;
      let lookupStdout = null;
      let lookupStderr = null;

      if (!resolvedPid && providerName) {
        lookupCommand = `orin3.remoteengine prov ps -n ${providerName}`;

        const lookupResult = await execFileAsync("orin3.remoteengine", ["prov", "ps", "-n", providerName]);
        lookupStdout = lookupResult.stdout;
        lookupStderr = lookupResult.stderr;
        resolvedPid = parsePidFromProviderList(lookupStdout);

        if (!resolvedPid) {
          return {
            content: [
              {
                type: "text",
                text: JSON.stringify(
                  {
                    status: "error",
                    message: "プロバイダー名から PID を特定できませんでした。",
                    providerName,
                    lookupCommand,
                    lookupStdout,
                    lookupStderr,
                  },
                  null,
                  2
                ),
              },
            ],
          };
        }
      }

      const terminateArgs = ["prov", "terminate", resolvedPid];
      const terminateCommand = `orin3.remoteengine ${terminateArgs.join(" ")}`;
      const { stdout, stderr } = await execFileAsync("orin3.remoteengine", terminateArgs);

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                status: "ok",
                providerName: providerName ?? null,
                requestedPid: pid ?? null,
                resolvedPid,
                lookupCommand,
                lookupStdout,
                lookupStderr,
                terminateCommand,
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
                providerName: providerName ?? null,
                requestedPid: pid ?? null,
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