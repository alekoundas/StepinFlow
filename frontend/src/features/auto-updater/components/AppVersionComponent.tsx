import { useAutoUpdaterStatus } from "@/features/auto-updater/hooks/use-auto-updater-status";

/** The version this is. Checking for a newer one is on Home. */
export default function AppVersionComponent() {
  const status = useAutoUpdaterStatus();

  if (!status) return null;

  return (
    <span
      className="block mt-3 pt-3 border-top-1 text-sm"
      style={{ paddingInline: 12, borderColor: "var(--surface-border)", color: "var(--text-color-secondary)" }}
    >
      StepinFlow {status.currentVersion}
    </span>
  );
}
