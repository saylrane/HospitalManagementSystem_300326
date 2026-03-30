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
    public class PrescriptionsController : ControllerBase
    {
        private readonly PrescriptionService _prescriptionService;

        public PrescriptionsController(PrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }
 
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<PrescriptionResponseDto>> CreatePrescription([FromBody] CreatePrescriptionDto dto)
        {
            try
            {
                var result = await _prescriptionService.CreatePrescriptionAsync(dto);
                return CreatedAtAction(nameof(GetPrescriptionById), new { id = result.Id }, result);
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
         
        [HttpGet]
        [Authorize(Roles = "Doctor,Receptionist")]
        public async Task<ActionResult<List<PrescriptionResponseDto>>> GetAllPrescriptions()
        {
            try
            {
                var result = await _prescriptionService.GetAllPrescriptionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor,Receptionist")]
        public async Task<ActionResult<PrescriptionResponseDto>> GetPrescriptionById(Guid id)
        {
            try
            {
                var result = await _prescriptionService.GetPrescriptionByIdAsync(id);
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
         
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<PrescriptionResponseDto>> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(new { message = "ID mismatch" });

                var result = await _prescriptionService.UpdatePrescriptionAsync(dto);
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
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<bool>> DeletePrescription(Guid id)
        {
            try
            {
                var result = await _prescriptionService.DeletePrescriptionAsync(id);
                return Ok(new { message = "Prescription deleted successfully" });
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
