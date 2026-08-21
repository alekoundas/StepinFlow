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
                .ForMember(x => x.FlowAreas, o => o.Ignore())
                .ForMember(x => x.FlowPoints, o => o.Ignore());

            // FlowStep
            CreateMap<FlowStep, FlowStepDto>();
            CreateMap<FlowStepDto, FlowStep>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.InvokedFlow, o => o.Ignore())
                .ForMember(x => x.FlowArea, o => o.Ignore())
                .ForMember(x => x.FlowPoint, o => o.Ignore())
                .ForMember(x => x.FlowPointEnd, o => o.Ignore())
                .ForMember(x => x.ParentFlowStep, o => o.Ignore())
                .ForMember(x => x.FlowStepReference, o => o.Ignore())
                .ForMember(x => x.FlowStepReferenceEnd, o => o.Ignore())
                .ForMember(x => x.ChildrenFlowSteps, o => o.Ignore())
                .ForMember(x => x.FlowStepReferences, o => o.Ignore())
                .ForMember(x => x.FlowStepReferencesEnd, o => o.Ignore())
                .ForMember(x => x.FlowStepImages, o => o.Ignore());

            // FlowArea
            CreateMap<FlowArea, FlowAreaDto>()
                .ForMember(x => x.FlowStepsCount, o => o.MapFrom(x => x.FlowSteps.Count()));
            CreateMap<FlowAreaDto, FlowArea>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore());

            // FlowPoint
            CreateMap<FlowPoint, FlowPointDto>()
                .ForMember(x => x.FlowStepsCount, o => o.MapFrom(x => x.FlowSteps.Count() + x.EndFlowSteps.Count()));
            CreateMap<FlowPointDto, FlowPoint>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.Flow, o => o.Ignore())
                .ForMember(x => x.FlowSteps, o => o.Ignore())
                .ForMember(x => x.EndFlowSteps, o => o.Ignore());

            // FlowStepImage
            CreateMap<FlowStepImage, FlowStepImageDto>();
            CreateMap<FlowStepImageDto, FlowStepImage>()
                .ForMember(x => x.CreatedOn, o => o.Ignore())
                .ForMember(x => x.FlowStep, o => o.Ignore());
        }
    }
}
