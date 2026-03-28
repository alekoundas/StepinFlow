import { useEffect, useState, type ReactNode } from "react";
import IconComponent from "@/components/core/icon-component";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypesDataGridComponent } from "@/features/flow-step/components/FlowStepTypesDataGridComponent";
import FlowStepWaitForm from "@/features/flow-step/components/forms/FlowStepWaitFormComponent";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowDto } from "@/shared/models/database/flow/flow-dto";
import { backendApiService } from "@/services/backend-api-service";

interface Props {
  // treeNodeDto: TreeNodeDto;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}
export function WorkflowContentComponent() {
  const {
    selectedTreeNode,
    selectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
    setSelectedFlowStepTypeToAdd,
  } = useWorkflowStore();

  const [loadedDto, setLoadedDto] = useState<FlowDto | FlowStepDto | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(false);

  const formMode: FormMode = selectedFlowStepTypeToAdd
    ? FormMode.ADD
    : FormMode.VIEW;

  // Fetch full DTO only for existing flow steps (VIEW mode)
  useEffect(() => {
    if (
      !selectedTreeNode ||
      selectedTreeNode.isNew ||
      selectedTreeNode.isFlow
    ) {
      setLoadedDto(null);
      return;
    }

    setIsLoading(true);
    backendApiService.FlowStep.get(+selectedTreeNode.key)
      .then((dto: FlowStepDto) => setLoadedDto(dto))
      .finally(() => setIsLoading(false));
  }, [selectedTreeNode]); // selectedFlowStepTypeToAdd is NOT a dep here – ADD is sync

  const handleSave = async (saveDto: FlowStepDto) => {
    if (formMode === FormMode.ADD) {
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
    } else {
      // TODO: implement UPDATE when you add EDIT mode
      console.warn("UPDATE not implemented yet");
    }
  };

  // ====================== RENDER FORMS ====================== //
  if (!selectedTreeNode) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent
          text="Select a node from the tree"
          size="lg"
        />
      </div>
    );
  }

  // 1. New node → type picker grid
  if (selectedTreeNode.isNew && !selectedFlowStepTypeToAdd) {
    return (
      <div className="m-4 mr-3">
        <FlowStepTypesDataGridComponent />
      </div>
    );
  }

  // 2. New node + type chosen → ADD form
  if (selectedTreeNode.isNew && selectedFlowStepTypeToAdd) {
    let formElement: ReactNode;
    switch (selectedFlowStepTypeToAdd) {
      case FlowStepTypeEnum.WAIT:
        formElement = (
          <FlowStepWaitForm
            formMode={formMode}
            onSubmit={handleSave}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                flowStepType: "WAIT",
                orderNumber: 22,
                name: "Wait",
                waitForMilliseconds: 50,
              })
            }
          />
        );
        break;

      case FlowStepTypeEnum.LOOP:
        formElement = <LabelComponent text="LOOP form – coming soon" />;
        break;

      default:
        formElement = (
          <LabelComponent
            text={`Form for type ${selectedFlowStepTypeToAdd} not implemented yet`}
          />
        );
    }

    return <div className="m-4 mr-3">{formElement}</div>;
  }

  // 3. Flow node
  if (selectedTreeNode.isFlow) {
    return (
      <div className="m-4 mr-3">
        <IconComponent name="plus" />
        {/* TODO: replace with real Flow form later */}
      </div>
    );
  }

  // 4. Existing flow step → VIEW
  if (isLoading) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent text="Loading flow step..." />
      </div>
    );
  }

  if (!loadedDto) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent
          text="Failed to load data"
          className="p-error"
        />
      </div>
    );
  }

  const flowStepDto = loadedDto as FlowStepDto;
  let formElement: ReactNode;

  switch (selectedTreeNode.flowStepType) {
    case FlowStepTypeEnum.WAIT:
      formElement = (
        <FlowStepWaitForm
          formMode={formMode}
          onSubmit={handleSave}
          defaultValues={flowStepDto}
        />
      );
      break;

    case FlowStepTypeEnum.LOOP:
      formElement = <LabelComponent text="LOOOOOOOOOP" />;
      break;

    default:
      formElement = <LabelComponent text="Unsupported flow step type" />;
  }

  return <div className="m-4 mr-3">{formElement}</div>;
}
