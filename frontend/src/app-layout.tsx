import type { MenuItem } from "primereact/menuitem";
import { Button } from "primereact/button";
import { Menu } from "primereact/menu";
import { useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import { classNames } from "primereact/utils";
import { ScrollPanel } from "primereact/scrollpanel";
import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import AiChatDialogComponent from "@/features/ai/components/AiChatDialogComponent";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";

export default function AppLayout() {
  const [isCollapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();

  const openAiChat = useAiChatStore((state) => state.open);

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
            wrap={false}
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
    menuItemTemplate("Sub-Flows", "sitemap", "/sub-flows"),
  ];

  const menuItemsBottom: MenuItem[] = [
    {
      template: () => (
        <div className={"flex justify-content-start"}>
          <Button
            onClick={openAiChat}
            className="p-button-text p-button-plain w-full pl-0 pr-0 gap-3"
          >
            <IconComponent
              name="sparkles"
              className={classNames("ml-3", isCollapsed && "mr-3")}
            />
            <LabelComponent
              text="Ask AI"
              size="lg"
              weight="semibold"
              hidden={isCollapsed}
              wrap={false}
              className={classNames(!isCollapsed && "mr-3")}
            />
          </Button>
        </div>
      ),
    },
    menuItemTemplate("Settings", "cog", "/settings"),
  ];

  return (
    <div className="flex">
      <div
        className="flex flex-column justify-content-between h-screen "
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
      <div className="w-full h-screen">
        <ScrollPanel className="h-full">
          <Outlet />
        </ScrollPanel>
      </div>

      <AiChatDialogComponent />
    </div>
  );
}
