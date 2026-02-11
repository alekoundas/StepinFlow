import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
// import "./index.css";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import App from "./App.tsx";
import AppLayout from "./components/layout/app-layout.tsx";
import HomePage from "./pages/home/home-page.tsx";

const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      {
        path: "/",
        element: <HomePage />,
        // loader: rootLoader,
        // children: [
        //   {
        //     path: "team",
        //     element: <Team />,
        //     loader: teamLoader,
        //   },
        // ],
      },
      { path: "app", element: <App /> },
    ],
  },
]);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
