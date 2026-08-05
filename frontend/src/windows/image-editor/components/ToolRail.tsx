/**
 * ToolRail — left hand tool picker.
 */

import { Button } from "primereact/button";
import type { EditorTool } from "@/windows/image-editor/types";

interface ToolDefinition {
  tool: EditorTool;
  icon: string;
  label: string;
  shortcut: string;
}

const TOOLS: ToolDefinition[] = [
  { tool: "hand", icon: "pi pi-arrows-alt", label: "Pan", shortcut: "H" },
  { tool: "crop-rect", icon: "pi pi-stop", label: "Rectangle crop", shortcut: "R" },
  { tool: "crop-lasso", icon: "pi pi-pencil", label: "Freehand lasso crop", shortcut: "L" },
  { tool: "crop-polygon", icon: "pi pi-share-alt", label: "Polygon lasso crop", shortcut: "P" },
  { tool: "eraser", icon: "pi pi-eraser", label: "Eraser (make transparent)", shortcut: "E" },
];

interface ToolRailProps {
  tool: EditorTool;
  onToolChange: (tool: EditorTool) => void;
}

export default function ToolRail({ tool, onToolChange }: ToolRailProps) {
  return (
    <div className="image-editor__rail">
      {TOOLS.map((definition) => (
        <Button
          key={definition.tool}
          icon={definition.icon}
          text={tool !== definition.tool}
          severity={tool === definition.tool ? "info" : "secondary"}
          className="image-editor__rail-button"
          onClick={() => onToolChange(definition.tool)}
          tooltip={`${definition.label} (${definition.shortcut})`}
          tooltipOptions={{ position: "right" }}
          aria-label={definition.label}
        />
      ))}
    </div>
  );
}
