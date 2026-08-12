using System.Net;
using Microsoft.AspNetCore.Mvc;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.Dtos.Auth;
using ProjectScientDay2568.Service;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly RoleService _role;

        public RoleController(RoleService role)
        {
            _role = role;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = new ApiResponse();
            try
            {
                var roles = await _role.GetAll();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = roles;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RoleDto role)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(role?.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Role name is required.");
                    return HandleResponse(response);
                }

                var created = await _role.Add(role);
                if (!created)
                {
                    response.StatusCode = HttpStatusCode.Conflict;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Cannot create role (maybe already exists).");
                    return HandleResponse(response);
                }

                response.StatusCode = HttpStatusCode.Created;
                response.Result = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // PUT: /api/role/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] string id, [FromBody] RoleUpdateDto roleUpdate)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roleUpdate?.UpdateName))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("UpdateName is required.");
                    return HandleResponse(response);
                }

                roleUpdate.Id = id;

                var updated = await _role.Update(roleUpdate);
                if (!updated)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Cannot update role (not found or duplicate).");
                    return HandleResponse(response);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.Result = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // DELETE: /api/role/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var response = new ApiResponse();
            try
            {
                var deleted = await _role.Delete(id);
                if (!deleted)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Cannot delete role (not found or assigned to users).");
                    return HandleResponse(response);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.Result = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }
    }
}
