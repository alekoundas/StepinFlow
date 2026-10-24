import { useEffect, useRef } from "react";

interface Props {
  accent: string;
}

// The ring's angle is a typed property so the browser can turn it smoothly. Registered from script
// because the app keeps its styles inline; a hot reload registers it again, which throws.
const RING_ANGLE = "--tree-ring-angle";
try {
  CSS.registerProperty({ name: RING_ANGLE, syntax: "<angle>", inherits: false, initialValue: "0deg" });
} catch {
  // Already registered.
}

/**
 * The picked row's ring, from the step's colour into the accent, turning once as it is picked.
 * Only the ring is painted, so the row shows through it whatever it sits on. It stays still when
 * the system asks for less motion.
 */
export function TreeSelectionRingComponent({ accent }: Props) {
  const ref = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const turn = ref.current?.animate([{ [RING_ANGLE]: "0deg" }, { [RING_ANGLE]: "360deg" }], {
      duration: 900,
      easing: "cubic-bezier(0.2, 0.7, 0.2, 1)",
    });

    return () => turn?.cancel();
  }, []);

  return (
    <span
      ref={ref}
      aria-hidden="true"
      className="absolute pointer-events-none"
      style={{
        inset: -2,
        padding: 2,
        borderRadius: 8,
        background: `conic-gradient(from var(${RING_ANGLE}, 0deg), ${accent}, var(--primary-color), color-mix(in srgb, var(--primary-color) 60%, white), ${accent})`,
        // The content box cut out of the whole box leaves the padding: the ring.
        mask: "linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0)",
        maskComposite: "exclude",
      }}
    />
  );
}
