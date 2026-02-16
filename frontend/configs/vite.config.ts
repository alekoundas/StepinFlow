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
              target: "19", // or your React version
            },
          ],
        ],
      },
    }),
  ],
  base: "./", // crucial for Electron (relative paths)
  build: {
    outDir: path.resolve(__dirname, "../../dist/frontend"),
    emptyOutDir: true, // clean before build
    sourcemap: true,
    rollupOptions: {
      // optional: if needed
    },
  },
});
