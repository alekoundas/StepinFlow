import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createHashRouter, RouterProvider } from "react-router-dom";
// import { PrimeReactProvider } from "@primereact/core";
import { PrimeReactProvider } from "primereact/api";

import AppLayout from "./components/layout/app-layout.tsx";
import HomePage from "./pages/home/home-page.tsx";

// Global CSS PrimeReact
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import "primereact/resources/themes/soho-dark/theme.css";

const router = createHashRouter([
  {
    element: <AppLayout />,
    children: [
      {
        path: "/",
        element: <HomePage />,
        // errorElement:
        // loader: rootLoader,
        // children: [
        //   {
        //     path: "team",
        //     element: <Team />,
        //     loader: teamLoader,
        //   },
        // ],
      },
      // { path: "app", element: <App /> },
    ],
  },
]);

// const theme = {
//   preset: Aura,
//   options: {
//     prefix: "p",
//     darkModeSelector: "system",
//     cssLayer: false,
//   },
// };

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {/* <PrimeReactProvider theme={theme}> */}
    {/* <ToastProvider>
        <ThemeProvider> */}
    <PrimeReactProvider>
      <RouterProvider router={router} />
    </PrimeReactProvider>
    {/* </ThemeProvider>
      </ToastProvider> */}
    {/* </PrimeReactProvider> */}
  </StrictMode>,
);
