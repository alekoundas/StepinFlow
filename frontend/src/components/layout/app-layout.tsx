import { Outlet } from "react-router-dom";

export default function AppLayout() {
  return (
    <div className="flex flex-row">
      <h1>Claim Grid</h1>
      <Outlet />
    </div>
  );
}
