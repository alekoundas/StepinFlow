import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createHashRouter, RouterProvider } from "react-router-dom";
import { PrimeReactProvider } from "primereact/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools"; // optional

// Global CSS PrimeReact
import "primeicons/primeicons.css";
import "primereact/resources/themes/soho-dark/theme.css";
import "primeflex/primeflex.css";

// Pages
import AppLayout from "@/app-layout";
import HomePage from "@/pages/home/home-page";
import { WorkflowPage } from "@/features/workflow/WorkflowPage";
import { FlowListPage } from "@/features/flow/FlowListPage";
import { FlowFormPage } from "@/features/flow/FlowFormPage";
import { DialogRootComponent } from "@/shared/components/modal-component/DialogRootComponent";
import SearchAreaOverlayPage from "@/windows/overlay/SearchAreaOverlayPage";

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
      {
        path: "/workflow/:id",
        element: <WorkflowPage />,
      },

      // { path: "app", element: <App /> },
    ],
  },
  {
    path: "/search-area-overlay",
    element: <SearchAreaOverlayPage />,
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

// TanStack Query(formerly React Query)
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes - data stays fresh
      gcTime: 10 * 60 * 1000, // 10 minutes - how long to keep in memory
      refetchOnWindowFocus: false, // prevents unnecessary refetches
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {/* <PrimeReactProvider theme={theme}> */}
    {/* <ToastProvider>
        <ThemeProvider> */}
    <QueryClientProvider client={queryClient}>
      <PrimeReactProvider>
        <RouterProvider router={router} />
        <DialogRootComponent />
        <ReactQueryDevtools initialIsOpen={false} /> {/* only shows in dev */}
      </PrimeReactProvider>
    </QueryClientProvider>
    {/* </ThemeProvider>
      </ToastProvider> */}
    {/* </PrimeReactProvider> */}
  </StrictMode>,
);
