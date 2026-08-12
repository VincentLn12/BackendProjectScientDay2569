using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BranchesController(ApplicationDbContext context)
        {
            _context = context;
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
                var branches = await _context.Branches.ToListAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = branches;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var response = new ApiResponse();
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลสาขาวิชา");
                    return NotFound(response);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = branch;
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
        public async Task<IActionResult> Post([FromBody] BranchDto model)
        {
            var response = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาระบุชื่อสาขาวิชา");
                    return BadRequest(response);
                }

                var branch = new Branch
                {
                    Name = model.Name
                };

                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = branch;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] BranchDto model)
        {
            var response = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาระบุชื่อสาขาวิชา");
                    return BadRequest(response);
                }

                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลสาขาวิชา");
                    return NotFound(response);
                }

                branch.Name = model.Name;

                _context.Branches.Update(branch);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = branch;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new ApiResponse();
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลสาขาวิชา");
                    return NotFound(response);
                }

                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = "ลบสาขาวิชาสำเร็จ";
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

    public class BranchDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
