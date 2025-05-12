using Microsoft.AspNetCore.Mvc;
using WebApplication4.Models;
using WebApplication4.Services;
using Microsoft.Data.SqlClient;

namespace WebApplication4.Controllers
{
    [ApiController]
    [Route("api/warehouse")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }
        
        [HttpPost("manual")]
        public async Task<IActionResult> AddProduct([FromBody] WarehouseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request data.");

            try
            {
                var id = await _service.AddProductToWarehouseAsync(request);
                return CreatedAtAction(nameof(AddProduct), new { id }, new { IdProductWarehouse = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message }); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message }); 
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message }); 
            }
        }
        
    }
}
