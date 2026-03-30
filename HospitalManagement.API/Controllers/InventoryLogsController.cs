using HospitalManagement.Application.DTOs;
using HospitalManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HospitalManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class InventoryLogsController : ControllerBase
    {
        private readonly InventoryService _inventoryService;

        public InventoryLogsController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
         
        [HttpGet]
        public async Task<ActionResult<List<InventoryResponseDto>>> GetAllInventoryLogs()
        {
            try
            {
                var result = await _inventoryService.GetAllInventoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryResponseDto>> GetInventoryLogById(Guid id)
        {
            try
            {
                var result = await _inventoryService.GetInventoryByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
 
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> DeleteInventoryLog(Guid id)
        {
            try
            {
                var result = await _inventoryService.DeleteInventoryAsync(id);
                return Ok(new { message = "Inventory log deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
