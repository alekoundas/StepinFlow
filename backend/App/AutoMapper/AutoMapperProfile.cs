using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;

namespace App.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Flow
            CreateMap<Flow, FlowDto>();
            CreateMap<FlowDto, Flow>();

            // FlowStep
            CreateMap<FlowStep, FlowStepDto>();
            CreateMap<FlowStepDto, FlowStep>();

            // FlowSearchArea
            CreateMap<FlowSearchArea, FlowSearchAreaDto>();
            CreateMap<FlowSearchAreaDto, FlowSearchArea>();

            // FlowStepImage
            CreateMap<FlowStepImage, FlowStepImageDto>();
            CreateMap<FlowStepImageDto, FlowStepImage>();

            // SubFlow
            CreateMap<SubFlow, SubFlowDto>();
            CreateMap<SubFlowDto, SubFlow>();

        }
    }
}
