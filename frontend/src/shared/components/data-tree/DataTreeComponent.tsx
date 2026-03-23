import { useEffect, useState, type ReactNode } from "react";
import {
  Tree,
  type TreeEventNodeEvent,
  type TreeNodeTemplateOptions,
  type TreeSelectionEvent,
} from "primereact/tree";
import { backendApiService } from "@/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";
import { classNames } from "primereact/utils";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { DataTreeFlowTemplate } from "@/features/flow/components/data-tree-templates/DataTreeFlowTemplate";
import IconComponent from "@/components/core/icon-component";

interface Props<T> {
  flowId: number;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}

export function DataTreeComponent<T>({ flowId }: Props<T>) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const { selectedTreeNode, setSelectedTreeNode } = useWorkflowStore();
  const [loading, setLoading] = useState(false);

  // useEffect(() => {
  //   getContentTemplate();
  // }, [selectedTreeNode]);

  const loadTreeChildren = async (id: number, isFlow: boolean) => {
    setLoading(true);
    try {
      let response: TreeNodeDto[];
      if (isFlow) {
        response = await backendApiService.Flow.getTreeNodes(id);
        setData(insertNewBubble(response));
      } else {
        // response = await backendApiService.FlowStep.getTreeNodes(id);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTreeChildren(flowId, true);
  }, []);

  const onExpand = async (e: TreeEventNodeEvent) => {
    if (!e.node.children)
      await loadTreeChildren(e.node.key ? +e.node.key : -1, false);
  };
  const onSelect = (e: TreeEventNodeEvent) => {
    setSelectedTreeNode(e.node as TreeNodeDto);
  };

  const insertNewBubble = (treeNodes: TreeNodeDto[]) => {
    const newChild: TreeNodeDto = new TreeNodeDto({
      key: -1,
      name: "New item",
      // flowStepType,
      // icon: "pi pi-file",
      isNew: true,
      leaf: true,
    });

    // Recursive adition
    // const addChildRecursive = (items: TreeNodeDto[]): TreeNodeDto[] =>
    const result = treeNodes.map(
      (item) =>
        item.flowStepType === FlowStepTypeEnum.SUB_FLOW
          ? { ...item, children: [...item.children, newChild] }
          : { ...item },
      // : {
      //     ...item,
      //     children: item.children
      //       ? addChildRecursive(item.children)
      //       : undefined,
      //   },
    );
    return result;
    // setData((prev) => addChildRecursive(prev));
  };

  const nodeTemplate = (
    treeNodeDto: TreeNodeDto,
    _options: TreeNodeTemplateOptions,
  ): ReactNode => {
    let template: ReactNode;
    const isSelected = selectedTreeNode?.key == treeNodeDto.key;

    if (treeNodeDto.isFlow) {
      template = <DataTreeFlowTemplate treeNode={treeNodeDto} />;
    } else if (treeNodeDto.isNew) {
      template = <IconComponent name="plus" />;
    } else
      switch (treeNodeDto.flowStepType) {
        case FlowStepTypeEnum.LOOP:
          template = <DataTreeFlowTemplate treeNode={treeNodeDto} />;
      }

    return (
      <div className=" flex w-full gap-2   cursor-pointer">
        <div className="flex w-full">{template}</div>
        {isSelected && !treeNodeDto.isNew && <IconComponent name="check" />}
      </div>
    );
  };

  return (
    <Tree
      value={data}
      onExpand={onExpand}
      selectionMode="single"
      // onSelect={onSelect}
      onSelect={onSelect}
      // onNodeDoubleClick={(e: TreeNodeDoubleClickEvent) =>
      //   (e.node.expanded = true)
      // }
      loading={loading}
      nodeTemplate={nodeTemplate}
    />
  );
}
