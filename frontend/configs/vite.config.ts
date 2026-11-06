import { defineConfig } from "vite";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import babel from "@rolldown/plugin-babel";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  base: "./", // crucial for Electron (relative paths)
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "../src"),
    },
  },
  build: {
    outDir: path.resolve(import.meta.dirname, "../../dist/frontend"),
    emptyOutDir: true, // clean before build
    sourcemap: true,
    rolldownOptions: {},
  },
  define: {
    "process.env": {}, // ← This is the fix
    // or more specific if you prefer:
    // 'process.env.REACT_APP_CSS_NONCE': JSON.stringify(''),
  },
});
