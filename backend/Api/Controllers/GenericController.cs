using AutoMapper;
using Business.DataService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Data;

namespace API.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class GenericController<TEntity, TEntityDto, TEntityAddDto> : ControllerBase
        where TEntity : class
        where TEntityDto : class
        where TEntityAddDto : class
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;
        private readonly IStringLocalizer _localizer;

        public GenericController(
            IMapper mapper,
            IDataService dataService,
            IStringLocalizer localizer)
        {
            _mapper = mapper;
            _dataService = dataService;
            _localizer = localizer;
        }


        // GET: api/controller/5
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntityDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<TEntityDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FilterByColumnEquals("Id", id).FirstOrDefaultAsync();
            TEntityDto entityDto = _mapper.Map<TEntityDto>(entity);
            if (entityDto == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntityDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);
            }

            return new ApiResponse<TEntityDto>().SetSuccessResponse(entityDto);
        }

        // POST: api/controller
        [HttpPost]
        public virtual async Task<ActionResult<ApiResponse<List<TEntity>>>> Post([FromBody] List<TEntityAddDto> entityDtos)
        {
            string className = typeof(TEntity).Name;

            if (!IsUserAuthorized("Add"))
                return new ApiResponse<List<TEntity>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            //if (entityDtos.Count() == 0)
            //    return BadRequest(new ApiResponse<List<TEntity>>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

            foreach (var entityDto in entityDtos)
                if (CustomValidatePOST(entityDto, out string[] errors))
                    return BadRequest(new ApiResponse<TEntity>().SetErrorResponse(errors));

            List<TEntity> entities = _mapper.Map<List<TEntity>>(entityDtos);

            int result = await _dataService.GetGenericRepository<TEntity>().AddRangeAsync(entities);
            if (result <= 0)
                return new ApiResponse<List<TEntity>>().SetErrorResponse(_localizer[TranslationKeys.An_error_occurred_while_creating_the_entity]);

            return new ApiResponse<List<TEntity>>().SetSuccessResponse(entities, _localizer[TranslationKeys._0_updated_successfully, className]);
        }

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Put(string? id, [FromBody] TEntityDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            string className = typeof(TEntity).Name;

            //if (!ModelState.IsValid)
            //    return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]);

            if (CustomValidatePUT(entityDto, out string[] errors))
                return BadRequest(new ApiResponse<TEntity>().SetErrorResponse(errors));

            TEntity entity = _mapper.Map<TEntity>(entityDto);


            TEntity? existingEntity = await _dataService.GetGenericRepository<TEntity>().FilterByColumnEquals("Id", id).FirstOrDefaultAsync();
            if (existingEntity == null)
                return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);

            _dataService.Update(entity);
            return new ApiResponse<TEntity>().SetSuccessResponse(entity, _localizer[TranslationKeys._0_updated_successfully, className]);
        }

        // DELETE: api/controller/5
        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Delete(string? id)
        {
            if (!IsUserAuthorized("Delete"))
                return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            string className = typeof(TEntity).Name;
            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FilterByColumnEquals("Id", id).FirstOrDefaultAsync();
            if (entity == null)
            {
                return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);
            }

            if (CustomValidateDELETE(entity, out string[] errors))
                return BadRequest(new ApiResponse<TEntity>().SetErrorResponse(errors));


            int result = await _dataService.GetGenericRepository<TEntity>().RemoveAsync(entity);
            if (result != 1)
                return new ApiResponse<TEntity>().SetErrorResponse(_localizer[TranslationKeys.An_error_occurred_while_deleting_the_entity]);

            return new ApiResponse<TEntity>().SetSuccessResponse(entity, _localizer[TranslationKeys._0_deleted_successfully, className]);
        }






        // POST: api/controller/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<TEntityDto>>> GetDataTable([FromBody] DataTableDto<TEntityDto> dataTable)
        {
            var query = _dataService.GetGenericRepository<TEntity>();
            DataTableQueryUpdate(query, dataTable);

            // Handle Sorting of DataTable.
            if (dataTable.Sorts.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.Sorts.First();
                string fieldName = dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length - 1);

                if (dataTableSort.Order > 0)
                    query.OrderBy(fieldName, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(fieldName, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.Sorts.Skip(1))
                {
                    fieldName = sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length - 1);
                    if (dataTableSort.Order > 0)
                        query.ThenBy(fieldName, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(fieldName, OrderDirectionEnum.DESCENDING);
                }
            }

            foreach (var filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    if (filter.FieldName == "UserId")
                        query.FilterByColumnEquals(filter.FieldName, new Guid(filter.Value));
                    else
                        query.FilterByColumnEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);

                //if (filter.FilterType == DataTableFiltersEnum.custom)
            }


            // Handle Pagging of DataTable.
            int skip = dataTable.Page * dataTable.Rows;
            int take = dataTable.Rows;
            query.AddPagging(skip, take);



            // Retrieve Data.
            List<TEntity> result = await query.ToListAsync();
            List<TEntityDto> resultDto = _mapper.Map<List<TEntityDto>>(result);

            foreach (var filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    if (filter.FieldName == "UserId")
                        query.FilterByColumnEquals(filter.FieldName, new Guid(filter.Value));
                    else
                        query.FilterByColumnEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);
            }


            int rowCount = await query.CountAsync();
            int totalRecords = rowCount;

            dataTable.Data = resultDto;
            dataTable.TotalRecords = totalRecords;
            dataTable.PageCount = (int)Math.Ceiling((double)totalRecords / dataTable.Rows);

            return new ApiResponse<DataTableDto<TEntityDto>>().SetSuccessResponse(dataTable);
        }



        protected virtual bool CustomValidatePOST(TEntityAddDto entity, out string[] errors)
        {
            errors = Array.Empty<string>();
            return false;
        }
        protected virtual bool CustomValidatePUT(TEntityDto entity, out string[] errors)
        {
            errors = Array.Empty<string>();
            return false;
        }
        protected virtual bool CustomValidateDELETE(TEntity entity, out string[] errors)
        {
            errors = Array.Empty<string>();
            return false;
        }

        protected virtual void DataTableQueryUpdate(IGenericRepository<TEntity> query, DataTableDto<TEntityDto> dataTable)
        {
        }


        protected virtual bool IsUserAuthorized(string action)
        {
            string controllerName = ControllerContext.ActionDescriptor.ControllerName;
            string claimName = controllerName + "_" + action;
            bool hasClaim = User.HasClaim("Permission", claimName);
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return hasClaim;
        }
    }
}