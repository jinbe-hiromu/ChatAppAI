import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { remoteengineProvPsTool } from "./tools/remoteengineProvPs.mjs";
import { remoteengineProvTerminateTool } from "./tools/remoteengineProvTerminate.mjs";

const server = new McpServer({
  name: "remoteengine-demo",
  version: "1.0.0",
});

server.tool(
  remoteengineProvPsTool.name,
  remoteengineProvPsTool.description,
  remoteengineProvPsTool.schema,
  remoteengineProvPsTool.handler
);

server.tool(
  remoteengineProvTerminateTool.name,
  remoteengineProvTerminateTool.description,
  remoteengineProvTerminateTool.schema,
  remoteengineProvTerminateTool.handler
);

await server.connect(new StdioServerTransport());
