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

// Global shared types
import {
  BroadcastTypeEnum,
  type RecordedInput,
} from "../../electron/shared/types";

// Services
import { ElectronApiService } from "@/shared/services/electron-api-service";

// Pages
import AppLayout from "@/app-layout";
import HomePage from "@/pages/home/home-page";
import WorkflowPage from "@/features/workflow/WorkflowPage";
import FlowListPage from "@/features/flow/FlowListPage";
import FlowFormPage from "@/features/flow/FlowFormPage";
import DialogRootComponent from "@/shared/components/modal-component/DialogRootComponent";
import OverlayCapturePage from "@/windows/overlay/SearchAreaOverlayPage";

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
    path: "/overlay-capture",
    element: <OverlayCapturePage />,
  },
  {
    path: "/overlay-preview",
    element: <OverlayCapturePage />,
  },
]);

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

console.log("============LISTENING BACKEND BROADCASTS: ============");
ElectronApiService.backendApi.OnBroadcast((msg) => {
  if (msg.type === BroadcastTypeEnum.OVERLAY_MOUSE_EVENT) {
    const recordedInput = msg.payload as RecordedInput;
    switch (recordedInput.type) {
      case "BUTTON_DOWN":
      case "BUTTON_UP":
        console.log(
          "Received backend broadcast - " +
            recordedInput.type +
            ", Button Type: " +
            recordedInput.cursorButtonType,
        );
        break;
      case "CURSOR_MOVE":
      case "CURSOR_DRAG":
        console.log(
          "Received backend broadcast - " +
            recordedInput.type +
            ", Physical Position: " +
            recordedInput.physicalX +
            ", " +
            recordedInput.physicalY,
        );
        break;
      case "KEY_UP":
      case "KEY_DOWN":
        console.log(
          "Received backend broadcast - " +
            recordedInput.type +
            ", Key Code: " +
            recordedInput.keyCode,
        );
        break;
      default:
        console.log("Received backend broadcast - OTHER_EVENT:", msg);
    }
  }
  // console.log("Received backend broadcast:", msg);
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
