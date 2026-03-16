import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createHashRouter, RouterProvider } from "react-router-dom";
import { PrimeReactProvider } from "primereact/api";

// Global CSS PrimeReact
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import "primereact/resources/themes/soho-dark/theme.css";

// Pages
import AppLayout from "@/components/layout/app-layout";
import HomePage from "@/pages/home/home-page";
import { FlowFormPage, FlowListPage } from "@/features/flow";

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
        element: <FlowListPage />,
      },
      {
        path: "/flows/new",
        element: <FlowFormPage />,
      },
      {
        path: "/flows/:id/view",
        element: <FlowFormPage />,
      },
      {
        path: "/flows/:id/edit",
        element: <FlowFormPage />,
      },
      {
        path: "/flows/:id/clone",
        element: <FlowFormPage />,
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
