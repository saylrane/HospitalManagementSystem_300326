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
    [Authorize(Roles = "Admin,Doctor")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientService _patientService;

        public PatientsController(PatientService patientService)
        {
            _patientService = patientService;
        }
         
        [HttpGet]
        public async Task<ActionResult<List<PatientResponseDto>>> GetAllPatients()
        {
            try
            {
                var result = await _patientService.GetAllPatientsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
         
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientResponseDto>> GetPatientById(Guid id)
        {
            try
            {
                var result = await _patientService.GetPatientByIdAsync(id);
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PatientResponseDto>> UpdatePatient(Guid id, [FromBody] UpdatePatientDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(new { message = "ID mismatch" });

                var result = await _patientService.UpdatePatientAsync(dto);
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
        public async Task<ActionResult<bool>> DeletePatient(Guid id)
        {
            try
            {
                var result = await _patientService.DeletePatientAsync(id);
                return Ok(new { message = "Patient deleted successfully" });
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
