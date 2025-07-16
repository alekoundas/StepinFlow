using Business.Services.Interfaces;
using Model.Models;

namespace Business.Services
{
    public class CloneService : ICloneService
    {
        private readonly IDataService _dataService;

        public CloneService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<FlowStep?> GetFlowStepClone(int flowStepId)
        {
            // Queues for processing different types
            Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue = new();
            Queue<(Flow Original, Flow Cloned)> flowQueue = new();
            Queue<(FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter)> flowParameterQueue = new();

            // Dictionaries to track cloned objects
            Dictionary<int, FlowStep> clonedFlowSteps = new();
            Dictionary<int, Flow> clonedFlows = new();
            Dictionary<int, FlowParameter> clonedFlowParameters = new();

            // Load the source FlowStep with all related data
            FlowStep? originalFlowStep = await _dataService.FlowSteps.LoadAllClone(flowStepId);

            if (originalFlowStep == null)
                return null;

            // Clone the root FlowStep and enqueue it
            FlowStep clonedFlowStep = CreateFlowStepClone(originalFlowStep);
            flowStepQueue.Enqueue((originalFlowStep, clonedFlowStep));
            clonedFlowSteps[originalFlowStep.Id] = clonedFlowStep;

            while (flowStepQueue.Count > 0)
            {
                // Process FlowSteps
                ProcessFlowSteps(flowStepQueue, flowQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);

                // Process Flows
                ProcessFlow(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlowParameters);

                // Process FlowParameters
                ProcessFlowParameter(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);
            }

            return clonedFlowStep;
        }

        public Flow? GetFlowClone(Flow flow)
        {
            // Queues for processing different types
            Queue<(Flow Original, Flow Cloned)> flowQueue = new();
            Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue = new();
            Queue<(FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter)> flowParameterQueue = new();

            // Dictionaries to track cloned objects
            Dictionary<int, Flow> clonedFlows = new();
            Dictionary<int, FlowStep> clonedFlowSteps = new();
            Dictionary<int, FlowParameter> clonedFlowParameters = new();

            // Load the source Flow with all related data
            List<Flow> originalFlows = new List<Flow>() { flow };
            Flow? originalFlow = originalFlows.FirstOrDefault();

            if (originalFlow == null)
                return null;

            // Clone the root Flow and enqueue it
            Flow clonedFlow = CreateFlowClone(originalFlow);
            flowQueue.Enqueue((originalFlow, clonedFlow));
            clonedFlows[originalFlow.Id] = clonedFlow;

            while (flowQueue.Count > 0 || flowStepQueue.Count > 0 || flowParameterQueue.Count > 0)
            {
                // Process Flows
                ProcessFlow(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlowParameters);

                // Process FlowParameters
                ProcessFlowParameter(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);

                // Process FlowSteps
                ProcessFlowSteps(flowStepQueue, flowQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);

            }

            return clonedFlow;
        }

        public async Task<Flow?> GetFlowClone(int flowId)
        {
            // Queues for processing different types
            Queue<(Flow Original, Flow Cloned)> flowQueue = new();
            Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue = new();
            Queue<(FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter)> flowParameterQueue = new();

            // Dictionaries to track cloned objects
            Dictionary<int, Flow> clonedFlows = new();
            Dictionary<int, FlowStep> clonedFlowSteps = new();
            Dictionary<int, FlowParameter> clonedFlowParameters = new();

            // Load the source Flow with all related data
            List<Flow> originalFlows = await _dataService.Flows.LoadAllExport(flowId);
            Flow? originalFlow = originalFlows.FirstOrDefault();

            if (originalFlow == null)
                return null;

            // Clone the root Flow and enqueue it
            Flow clonedFlow = CreateFlowClone(originalFlow);
            flowQueue.Enqueue((originalFlow, clonedFlow));
            clonedFlows[originalFlow.Id] = clonedFlow;

            while (flowQueue.Count > 0 || flowStepQueue.Count > 0 || flowParameterQueue.Count > 0)
            {
                // Process Flows
                ProcessFlow(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlowParameters);

                // Process FlowParameters
                ProcessFlowParameter(flowStepQueue, flowQueue, flowParameterQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);

                // Process FlowSteps
                ProcessFlowSteps(flowStepQueue, flowQueue, clonedFlowSteps, clonedFlows, clonedFlowParameters);

            }

            return clonedFlow;
        }

        private void ProcessFlowSteps(Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue, Queue<(Flow Original, Flow Cloned)> flowQueue, Dictionary<int, FlowStep> clonedFlowSteps, Dictionary<int, Flow> clonedFlows, Dictionary<int, FlowParameter> clonedFlowParameters)
        {
            while (flowStepQueue.Count > 0)
            {
                var (originalFS, clonedFS) = flowStepQueue.Dequeue();

                // Clone SubFlow
                if (originalFS.SubFlow != null && originalFS.IsSubFlowReferenced == false)
                {
                    Flow clonedFlow = CreateFlowClone(originalFS.SubFlow);
                    clonedFS.SubFlow = clonedFlow;
                    flowQueue.Enqueue((originalFS.SubFlow, clonedFlow));
                    clonedFlows[originalFS.SubFlow.Id] = clonedFlow;
                }
                else if (originalFS.SubFlowId != null)
                {
                    clonedFS.SubFlowId = originalFS.SubFlowId;
                }


                // Clone FlowParameter
                if (originalFS.FlowParameterId != null && clonedFlowParameters.ContainsKey(originalFS.FlowParameterId.Value))
                    clonedFS.FlowParameter = clonedFlowParameters[originalFS.FlowParameterId.Value];
                else if (originalFS.FlowParameterId != null)
                    clonedFS.FlowParameterId = originalFS.FlowParameterId;

                // Process ChildrenFlowSteps
                foreach (var child in originalFS.ChildrenFlowSteps)
                {
                    FlowStep? clonedParentTSFS = child.ParentTemplateSearchFlowStepId.HasValue
                        ? clonedFlowSteps.GetValueOrDefault(child.ParentTemplateSearchFlowStepId.Value)
                        : null;

                    FlowStep clonedChild = CreateFlowStepClone(child, clonedFS, clonedParentTSFS);
                    clonedFS.ChildrenFlowSteps.Add(clonedChild);
                    flowStepQueue.Enqueue((child, clonedChild));
                    clonedFlowSteps[child.Id] = clonedChild;
                }

                // Process ChildrenTemplateSearchFlowSteps
                foreach (var child in originalFS.ChildrenTemplateSearchFlowSteps)
                {
                    FlowStep clonedChild = CreateFlowStepClone(child);
                    clonedFS.ChildrenTemplateSearchFlowSteps.Add(clonedChild);
                    flowStepQueue.Enqueue((child, clonedChild));
                    clonedFlowSteps[child.Id] = clonedChild;
                }
            }
        }

        private void ProcessFlow(Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue, Queue<(Flow Original, Flow Cloned)> flowQueue, Queue<(FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter)> flowParameterQueue, Dictionary<int, FlowStep> clonedFlowSteps, Dictionary<int, FlowParameter> clonedFlowParameters)
        {
            while (flowQueue.Count > 0)
            {
                var (originalF, clonedF) = flowQueue.Dequeue();

                // Clone FlowParameter
                if (originalF.FlowParameter != null && !clonedFlowParameters.ContainsKey((int)originalF.FlowParameter.Id))
                {
                    FlowParameter clonedFP = CreateFlowParameterClone(originalF.FlowParameter);
                    clonedF.FlowParameter = clonedFP;
                    flowParameterQueue.Enqueue(((FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter))(originalF.FlowParameter, clonedFP, null));
                    clonedFlowParameters[(int)originalF.FlowParameter.Id] = clonedFP;
                }

                // Clone FlowStep
                if (originalF.FlowStep != null && !clonedFlowSteps.ContainsKey(originalF.FlowStep.Id))
                {
                    FlowStep clonedFS = CreateFlowStepClone(originalF.FlowStep);
                    clonedF.FlowStep = clonedFS;
                    flowStepQueue.Enqueue((originalF.FlowStep, clonedFS));
                    clonedFlowSteps[originalF.FlowStep.Id] = clonedFS;
                }


                // Clone ParentFlowStep results in a circular reference. Fixed by appliyng ids after save.
                //if (originalF.ParentSubFlowStepId != null && clonedFlowSteps.ContainsKey(originalF.ParentSubFlowStepId.Value))
                //    clonedF.ParentSubFlowStep = clonedFlowSteps[originalF.ParentSubFlowStepId.Value];
            }
        }

        private void ProcessFlowParameter(Queue<(FlowStep Original, FlowStep Cloned)> flowStepQueue, Queue<(Flow Original, Flow Cloned)> flowQueue, Queue<(FlowParameter Original, FlowParameter Cloned, FlowParameter? ParentFlowParameter)> flowParameterQueue, Dictionary<int, FlowStep> clonedFlowSteps, Dictionary<int, Flow> clonedFlows, Dictionary<int, FlowParameter> clonedFlowParameters)
        {
            while (flowParameterQueue.Count > 0)
            {
                var (originalFP, clonedFP, parentFP) = flowParameterQueue.Dequeue();

                clonedFP.ParentFlowParameter = parentFP;

                if (originalFP.Flow != null)
                {
                    clonedFP.Flow = clonedFlows[originalFP.Flow.Id];
                }


                // Clone ChildrenFlowParameters
                foreach (var childFP in originalFP.ChildrenFlowParameters)
                {
                    if (!clonedFlowParameters.ContainsKey(childFP.Id))
                    {
                        FlowParameter clonedChildFP = CreateFlowParameterClone(childFP);
                        clonedFP.ChildrenFlowParameters.Add(clonedChildFP);
                        flowParameterQueue.Enqueue((childFP, clonedChildFP, clonedFP));
                        clonedFlowParameters[childFP.Id] = clonedChildFP;
                    }
                    else // REMOVE?
                    {
                        clonedFP.ChildrenFlowParameters.Add(clonedFlowParameters[childFP.Id]);
                    }
                }
            }
        }

        private FlowStep CreateFlowStepClone(FlowStep original, FlowStep? parentFlowStep = null, FlowStep? parentTemplateSearchFlowStep = null)
        {
            return new FlowStep
            {
                ParentFlowStep = parentFlowStep,
                ParentTemplateSearchFlowStep = parentTemplateSearchFlowStep,

                Name = original.Name,
                ProcessName = original.ProcessName,
                IsExpanded = original.IsExpanded,
                IsSelected = false,
                OrderingNum = original.OrderingNum,
                Type = original.Type,
                TemplateMatchMode = original.TemplateMatchMode,
                IsSubFlowReferenced = original.IsSubFlowReferenced,
                TemplateImage = original.TemplateImage != null ? (byte[])original.TemplateImage.Clone() : null,
                Accuracy = original.Accuracy,
                RemoveTemplateFromResult = original.RemoveTemplateFromResult,
                CursorAction = original.CursorAction,
                CursorButton = original.CursorButton,
                CursorScrollDirection = original.CursorScrollDirection,
                CursorRelocationType = original.CursorRelocationType,
                Milliseconds = original.Milliseconds,
                //WaitForHours = original.WaitForHours,
                //WaitForMinutes = original.WaitForMinutes,
                //WaitForSeconds = original.WaitForSeconds,
                //WaitForMilliseconds = original.WaitForMilliseconds,
                Height = original.Height,
                Width = original.Width,
                IsLoop = original.IsLoop,
                IsLoopInfinite = original.IsLoopInfinite,
                LoopCount = original.LoopCount,
                LoopMaxCount = original.LoopMaxCount,
                LoopTime = original.LoopTime,
                LocationX = original.LocationX,
                LocationY = original.LocationY,
                
            };
        }

        private Flow CreateFlowClone(Flow original)
        {
            return new Flow
            {
                Name = original.Name,
                Type = original.Type,
                IsSelected = false,
                IsExpanded = original.IsExpanded,
                OrderingNum = original.OrderingNum
            };
        }

        private FlowParameter CreateFlowParameterClone(FlowParameter original)
        {
            return new FlowParameter
            {
                Name = original.Name,
                IsExpanded = original.IsExpanded,
                IsSelected = false,
                OrderingNum = original.OrderingNum,
                Type = original.Type,
                TemplateSearchAreaType = original.TemplateSearchAreaType,
                ProcessName = original.ProcessName,
                SystemMonitorDeviceName = original.SystemMonitorDeviceName,
                LocationTop = original.LocationTop,
                LocationLeft = original.LocationLeft,
                LocationRight = original.LocationRight,
                LocationBottom = original.LocationBottom
            };
        }
    }
}