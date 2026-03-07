import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { PanelMenu } from "primereact/panelmenu";
import { Sidebar } from "primereact/sidebar";
import { ReactElement, useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import IconComponent from "../core/icon-component/icon-component";

export default function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();

  // const menuItemTemplate = (title: string, icon: string): MenuItem => ({
  //   template: () => (
  //     <div className="flex justify-content-center m-4 gap-3">
  //       <i className={`pi pi-${icon}`} />
  //       <label hidden={collapsed}>{title}</label>
  //     </div>
  //   ),
  // });

  const menuItemTemplate = (title: string, icon: string): MenuItem => ({
    template: () => (
      <div className="flex justify-content-center mr-4 ml-4">
        <Button
          onClick={() => setCollapsed(!collapsed)}
          className="p-button-text p-button-plain gap-3"
        >
          <IconComponent
            name={icon}
          />
          <p hidden={collapsed}>{title}</p>
        </Button>
      </div>
    ),
  });

  const menuItems: MenuItem[] = [
    {
      template: () => (
        <div className="flex justify-content-center">
          <Button
            icon="pi pi-bars"
            onClick={() => setCollapsed(!collapsed)}
            className="p-button-text p-button-plain "
          />
        </div>
      ),
    },

    {
      separator: true,
    },
    menuItemTemplate("Home", "home"),
    {
      label: collapsed ? "Home" : undefined,
      icon: "pi pi-home",
      className: "ml-1",

      command: () => navigate("/"),
    },
    {
      label: collapsed ? "Settings" : undefined,
      icon: "pi pi-cog",
      className: "pl-1",
      command: () => navigate("/settings"),
    },
  ];

  return (
    <div className="flex h-full">
      <div className="w-auto  h-full">
        {/* <Sidebar
          visible={true} 
          onHide={() => {}} 
          position="left"
          className="h-screen" // or style={{ height: '100vh' }}
          dismissable={false} // important for persistent
          showCloseIcon={false}
        > */}
        {/* your menu content or another Menu/PanelMenu inside */}
        <Menu
          model={menuItems}
          className={"border-none h-screen w-full"} // w-full set menu to smaller width??? grok explain
        />
        {/* </Sidebar> */}
      </div>
      <div className="w-full">
        <Outlet />
      </div>
    </div>
  );
}
