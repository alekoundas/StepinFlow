import { useEffect, useState, type ReactNode } from "react";
import {
  Tree,
  type TreeExpandedKeysType,
  type TreeNodeDoubleClickEvent,
  type TreeNodeTemplateOptions,
} from "primereact/tree";
import { backendApiService } from "@/services/backend-api-service";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { TreeNodeDto } from "@/shared/models/database/tree-node-dto";
import { classNames } from "primereact/utils";

interface Props<T> {
  flowId: number;
  // loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  // itemTemplate: (item: T) => ReactNode;
}

export function DataTreeComponent<T>({ flowId }: Props<T>) {
  const [data, setData] = useState<TreeNodeDto[]>([]);
  const [loading, setLoading] = useState(false);

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

  const onExpand = async (e: any) => {
    if (!e.node.children) await loadTreeChildren(e.node.key, false);
  };

  const insertNewBubble = (treeNodes: TreeNodeDto[]) => {
    const newChild: TreeNodeDto = new TreeNodeDto({
      key: -1,
      name: "New item",
      flowStepType: FlowStepTypeEnum.NEW,
      // icon: "pi pi-file",
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
    const canAdd =
      treeNodeDto.flowStepType === FlowStepTypeEnum.LOOP ||
      treeNodeDto.flowStepType === FlowStepTypeEnum.SUCCESS ||
      treeNodeDto.flowStepType === FlowStepTypeEnum.FAILURE;

    return (
      <div className="cursor-pointer flex items-center justify-between w-full gap-0 ">
        {/* Left side: icon + label (PrimeReact usually handles this) */}
        <div className="flex items-center gap-2 flex-1 min-w-0">
          {/* {treeNodeDto.icon && <i className={treeNodeDto.icon} />} */}
          <span className={classNames({ "font-medium": !treeNodeDto.leaf })}>
            {treeNodeDto.name}
          </span>
        </div>

        {/* Right side: conditional + bubble */}
        {canAdd && (
          <button
            type="button"
            // onClick={handleAdd}
            className="
            inline-flex items-center justify-center
            w-6 h-6 rounded-full
            bg-surface-200 hover:bg-primary-500/20
            text-surface-600 hover:text-primary-600
            transition-colors duration-150
            opacity-0 group-hover:opacity-100 focus:opacity-100
            focus:outline-none focus:ring-2 focus:ring-primary-400
          "
            aria-label={`Add child to ${treeNodeDto.name}`}
          >
            <i className="pi pi-plus text-xs" />
          </button>
        )}
      </div>
    );
  };

  return (
    <Tree
      value={data}
      onExpand={onExpand}
      // onNodeDoubleClick={(e: TreeNodeDoubleClickEvent) =>
      //   (e.node.expanded = true)
      // }
      loading={loading}
      nodeTemplate={nodeTemplate}
    />
  );
}
