import { useEffect, useState, type ReactNode } from "react";
import IconComponent from "@/components/core/icon-component";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypesDataGridComponent } from "@/features/flow-step/components/FlowStepTypesDataGridComponent";
import FlowStepWaitForm from "@/features/flow-step/components/forms/FlowStepWaitFormComponent";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { backendApiService } from "@/services/backend-api-service";

interface Props {
  // treeNodeDto: TreeNodeDto;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}
export function WorkflowContentComponent({}: Props) {
  const {
    selectedTreeNode,
    selectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
    setSelectedFlowStepTypeToAdd,
  } = useWorkflowStore();
  const [contentTemplate, setContentTemplate] = useState<ReactNode>(<></>);

  // always up-to-date
  const formMode: FormMode = selectedFlowStepTypeToAdd
    ? FormMode.ADD
    : FormMode.VIEW;

  useEffect(() => {
    getContentTemplate();
  }, [selectedTreeNode, selectedFlowStepTypeToAdd]);

  const onSave = async (saveDto: FlowStepDto) => {
    if (formMode === "ADD") {
      const result = await backendApiService.FlowStep.create(saveDto);

      if (saveDto.parentFlowStepId) {
        setTreeRefreshTrigger({
          id: saveDto.parentFlowStepId,
          isFlow: false,
          selectNodeIdAfterLoad: result.newId,
        });
      }
      if (saveDto.flowId) {
        setTreeRefreshTrigger({
          id: saveDto.flowId,
          isFlow: true,
          selectNodeIdAfterLoad: result.newId,
        });
      }

      setSelectedFlowStepTypeToAdd(undefined);
    }
  };

  const getContentTemplate = (): void => {
    let contentTemplate: ReactNode;
    if (selectedTreeNode?.isFlow) {
      contentTemplate = <IconComponent name="plus" />;
    } else if (selectedTreeNode?.isNew && !selectedFlowStepTypeToAdd) {
      contentTemplate = <FlowStepTypesDataGridComponent />;
    } else
      switch (selectedTreeNode?.flowStepType || selectedFlowStepTypeToAdd) {
        case FlowStepTypeEnum.LOOP:
          contentTemplate = <LabelComponent text="LOOOOOOOOOP" />;
          break;
        case FlowStepTypeEnum.WAIT:
          contentTemplate = (
            <FlowStepWaitForm
              formMode={formMode}
              onSubmit={onSave}
              defaultValues={
                new FlowStepDto({
                  flowId: selectedTreeNode?.parentFlowId,
                  parentFlowStepId: selectedTreeNode?.parentFlowStepId,
                  flowStepType: "WAIT",
                  orderNumber: 22,
                  name: "Wait",
                  waitForMilliseconds: 50,
                })
              }
            />
          );
          break;
      }

    setContentTemplate(contentTemplate);
  };

  return <div className="m-4 mr-3">{contentTemplate}</div>;
}
