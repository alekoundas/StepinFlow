import { Button } from "primereact/button";
import { Menu } from "primereact/menu";
import type { MenuItem } from "primereact/menuitem";
import { useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import IconComponent from "../core/icon-component";
import LabelComponent from "../core/label-component";
import { classNames } from "primereact/utils";

export default function AppLayout() {
  const [isCollapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();

  const menuItemTemplate = (
    title: string,
    icon: string,
    navigateTo: string,
  ): MenuItem => ({
    template: () => (
      <div className={"flex justify-content-start"}>
        <Button
          onClick={() => navigate(navigateTo)}
          className="p-button-text p-button-plain  w-full pl-0 pr-0 gap-3"
        >
          <IconComponent
            name={icon}
            className={classNames("ml-3", isCollapsed && "mr-3")}
          />
          <LabelComponent
            text={title}
            size="lg"
            weight="semibold"
            hidden={isCollapsed}
            className={classNames(!isCollapsed && "mr-3")}
          />
        </Button>
      </div>
    ),
  });

  const menuItemsTop: MenuItem[] = [
    {
      template: () => (
        <div className={" "}>
          <Button
            onClick={() => setCollapsed(!isCollapsed)}
            className="p-button-text p-button-plain  w-full pl-0 pr-0 justify-content-start"
          >
            <IconComponent
              name={"bars"}
              className={classNames("ml-3", isCollapsed && "mr-3")}
            />
          </Button>
        </div>
      ),
    },
    {
      separator: true,
    },
    menuItemTemplate("Home", "home", "/"),
    menuItemTemplate("Flows", "cog", "/flows"),
    menuItemTemplate("Sub-Flows", "cog", "/settings"),
  ];

  const menuItemsBottom: MenuItem[] = [
    menuItemTemplate("Settings", "cog", "/settings"),
  ];

  return (
    <div className="flex">
      <div
        className="flex flex-column justify-content-between h-screen "
        // style={{ backgroundColor: "var(--bg)" }}
      >
        <Menu
          model={menuItemsTop}
          className={"border-noround w-full h-full"}
        />
        <Menu
          model={menuItemsBottom}
          className={"border-noround w-full"}
        />
      </div>
      <div className="">
        <Outlet />
      </div>
    </div>
  );
}
