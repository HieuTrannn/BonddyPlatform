using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.ContactDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers
{
    [Route("api/[controller]")]
    public class ContactsController : ApiControllerBase
    {
        private readonly IContactService _service;

        public ContactsController(IContactService service)
        {
            _service = service;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all contacts", Description = "Returns a list of all contacts.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<ContactResponseDto>>))]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Success(result);
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "Get contacts with paging", Description = "Returns contacts with search, sort, and paging. Query: Search, Page, PageSize, SortBy, SortOrder, CreatedFrom, CreatedTo.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<ContactResponseDto>>))]
        public async Task<IActionResult> GetPaged([FromQuery] ContactSearchRequestDto request)
        {
            var result = await _service.GetPagedAsync(request);
            return Success(result);
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(Summary = "Get contact by ID", Description = "Returns a single contact by ID.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(404, "Contact not found", typeof(ApiResponse))]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound("Contact not found") : Success(result);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create contact", Description = "Creates a new contact. Gmail and phone number must be unique.")]
        [SwaggerResponse(201, "Contact created", typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(400, "Bad request (e.g. Gmail or phone already exists)", typeof(ApiResponse))]
        public async Task<IActionResult> Create([FromBody] ContactCreateRequestDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created, "Contact created successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update contact", Description = "Updates an existing contact by ID.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(404, "Contact not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
        public async Task<IActionResult> Update(int id, [FromBody] ContactUpdateRequestDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Success(result, "Contact updated successfully");
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete contact", Description = "Deletes a contact by ID.")]
        [SwaggerResponse(204, "No content")]
        [SwaggerResponse(404, "Contact not found", typeof(ApiResponse))]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
