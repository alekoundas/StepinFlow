import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react({
      babel: {
        plugins: [
          [
            "babel-plugin-react-compiler",
            {
              // optional config – start minimal
              target: "19", // or your React version
            },
          ],
        ],
      },
    }),
  ],
});
