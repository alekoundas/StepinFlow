import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react({
      babel: {
        plugins: [
          [
            "babel-plugin-react-compiler",
            {
              target: "19",
            },
          ],
        ],
      },
    }),
  ],
  base: "./", // crucial for Electron (relative paths)
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "../src"),
    },
  },
  build: {
    outDir: path.resolve(__dirname, "../../dist/frontend"),
    emptyOutDir: true, // clean before build
    sourcemap: true,
    rollupOptions: {},
  },
  define: {
    "process.env": {}, // ← This is the fix
    // or more specific if you prefer:
    // 'process.env.REACT_APP_CSS_NONCE': JSON.stringify(''),
  },
});
