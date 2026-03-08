import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createHashRouter, RouterProvider } from "react-router-dom";
import { PrimeReactProvider } from "primereact/api";

// Global CSS PrimeReact
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import "primereact/resources/themes/soho-dark/theme.css";

// Pages
import AppLayout from "./components/layout/app-layout.tsx";
import HomePage from "./pages/home/home-page.tsx";
import FlowsPage from "./pages/flow/flows-page.tsx";
import { setupResponseListener } from "./services/backend-api-service.tsx";

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
      {
        path: "/flows",
        element: <FlowsPage />,
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

// Listen for .NET responses
setupResponseListener();

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
