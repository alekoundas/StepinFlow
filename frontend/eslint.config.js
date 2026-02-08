import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import tseslint from "typescript-eslint";
import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig([
  globalIgnores(["dist"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },

    // Enable React Hooks + Compiler rules
    plugins: {
      "react-hooks": reactHooks,
    },
    rules: {
      // Classic Hooks rules
      "react-hooks/rules-of-hooks": "error",
      "react-hooks/exhaustive-deps": "warn",

      // Compiler-specific safety rules (these catch code that breaks optimization)
      "react-hooks/config": "error", // Enforces compiler-compatible patterns
      "react-hooks/error-boundaries": "error", // Proper error boundary usage
      "react-hooks/component-hook-factories": "error", // Prevents misuse in factories
      "react-hooks/gating": "error", // Catches gating issues
      "react-hooks/globals": "error", // Globals that interfere
      // ... more may appear in future releases — check release notes
    },
  },

  //  Apply only to React/TSX files if you have non-React code
  // {
  //   files: ['**/*.{jsx,tsx,js,ts}'],
  // },
]);
