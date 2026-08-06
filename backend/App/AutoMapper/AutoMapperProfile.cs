using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;

namespace App.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        // Entity -> Dto maps are free to carry navigations, the JSON serializer handles the cycles.
        //
        // Dto -> Entity maps must NEVER carry them: the client round-trips whatever the get handler
        // returned, so a populated navigation would make EF re-insert or overwrite unrelated rows.
        // Handlers own child collections explicitly instead.
        public AutoMapperProfile()
        {
            // Flow
            CreateMap<Flow, FlowDto>();
            CreateMap<FlowDto, Flow>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore())
                .ForMember(x => x.FlowSearchAreas, o => o.Ignore())
                .ForMember(x => x.FlowLocations, o => o.Ignore());

            // FlowStep
            CreateMap<FlowStep, FlowStepDto>();
            CreateMap<FlowStepDto, FlowStep>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.SubFlow, o => o.Ignore())
                .ForMember(x => x.FlowSearchArea, o => o.Ignore())
                .ForMember(x => x.FlowLocation, o => o.Ignore())
                .ForMember(x => x.FlowLocationEnd, o => o.Ignore())
                .ForMember(x => x.ParentFlowStep, o => o.Ignore())
                .ForMember(x => x.FlowStepReference, o => o.Ignore())
                .ForMember(x => x.FlowStepReferenceEnd, o => o.Ignore())
                .ForMember(x => x.ChildrenFlowSteps, o => o.Ignore())
                .ForMember(x => x.FlowStepReferences, o => o.Ignore())
                .ForMember(x => x.FlowStepReferencesEnd, o => o.Ignore())
                .ForMember(x => x.FlowStepImages, o => o.Ignore());

            // FlowSearchArea
            CreateMap<FlowSearchArea, FlowSearchAreaDto>()
                .ForMember(x => x.FlowStepsCount, o => o.MapFrom(x => x.FlowSteps.Count()));
            CreateMap<FlowSearchAreaDto, FlowSearchArea>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore());

            // FlowLocation
            CreateMap<FlowLocation, FlowLocationDto>()
                .ForMember(x => x.FlowStepsCount, o => o.MapFrom(x => x.FlowSteps.Count() + x.EndFlowSteps.Count()));
            CreateMap<FlowLocationDto, FlowLocation>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore())
                .ForMember(x => x.EndFlowSteps, o => o.Ignore());

            // FlowStepImage
            CreateMap<FlowStepImage, FlowStepImageDto>();
            CreateMap<FlowStepImageDto, FlowStepImage>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.FlowStep, o => o.Ignore());

            // SubFlow
            CreateMap<SubFlow, SubFlowDto>();
            CreateMap<SubFlowDto, SubFlow>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore());
        }
    }
}
