import { useEffect, useState, type ReactNode } from "react";
import IconComponent from "@/components/core/icon-component";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypesDataGridComponent } from "@/features/flow-step/components/FlowStepTypesDataGridComponent";

interface Props {
  // treeNodeDto: TreeNodeDto;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}
export function WorkflowContentComponent({}: Props) {
  const { selectedTreeNode } = useWorkflowStore();
  const [contentTemplate, seContentTemplate] = useState<ReactNode>(<></>);

  useEffect(() => {
    getContentTemplate();
  }, [selectedTreeNode]);

  const getContentTemplate = (): void => {
    let contentTemplate: ReactNode;
    if (selectedTreeNode?.isFlow) {
      contentTemplate = <IconComponent name="plus" />;
    } else if (selectedTreeNode?.isNew) {
      contentTemplate = <FlowStepTypesDataGridComponent />;
    } else
      switch (selectedTreeNode?.flowStepType) {
        case FlowStepTypeEnum.LOOP:
          contentTemplate = <IconComponent name="plus" />;
      }
    seContentTemplate(contentTemplate);
  };

  return <div className="m-4 mr-3">{contentTemplate}</div>;
}
