using App.AutoMapper;
using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Tests.AutoMapper
{
    /// <summary>What a form's DTO may and may not set on an entity when it is mapped back.</summary>
    public sealed class AutoMapperProfileTests
    {
        private readonly IMapper _mapper;

        public AutoMapperProfileTests()
        {
            MapperConfiguration configuration = new MapperConfiguration(x => x.AddProfile<AutoMapperProfile>(), NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();
        }

        // The form sends no Id, and the empty Guid it arrived as broke the unique index.
        [Fact]
        public void A_flow_from_a_form_keeps_its_own_Id()
        {
            Guid sent = Guid.NewGuid();

            Flow flow = _mapper.Map<Flow>(new FlowDto { Name = "Login", PublicId = sent });

            flow.PublicId.ShouldNotBe(sent);
            flow.PublicId.ShouldNotBe(Guid.Empty);
        }
    }
}
