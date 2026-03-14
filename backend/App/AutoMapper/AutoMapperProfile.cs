using AutoMapper;
using Core.Models;
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
            CreateMap<FlowCreateDto, Flow>();

            // FlowStep
            CreateMap<FlowStep, FlowStepDto>(); 
            CreateMap<FlowStepCreateDto, FlowStep>();

            // FlowSearchArea
            CreateMap<FlowSearchArea, FlowSearchAreaDto>();
            CreateMap<FlowSearchAreaCreateDto, FlowSearchArea>();

            // FlowStepImage
            CreateMap<FlowStepImage, FlowStepImageDto>();
            CreateMap<FlowStepImageCreateDto, FlowStepImage>();

            // SubFlow
            CreateMap<SubFlow, SubFlowDto>();
            CreateMap<SubFlowCreateDto, SubFlow>();

        }
    }
}
