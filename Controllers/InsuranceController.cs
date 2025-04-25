using CarInsuranceSalesBot.Models;
using CarInsuranceSalesBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsuranceSalesBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsuranceController : ControllerBase
{
    private readonly InsuranceService _insuranceService;
        private readonly UserSessionManager _sessionManager;

        public InsuranceController(InsuranceService insuranceService, UserSessionManager sessionManager)
        {
            _insuranceService = insuranceService;
            _sessionManager = sessionManager;
        }

        [HttpPost("extractDocument")]
        public async Task<IActionResult> ExtractDocument([FromBody] DocumentExtractionRequest request)
        {
            if (string.IsNullOrEmpty(request.DocumentType) || string.IsNullOrEmpty(request.DocumentImageBase64))
            {
                return BadRequest("Document type and image data are required");
            }
            
            // For demo purposes, we'll return mock data
            
            if (request.DocumentType.ToLower() == "passport")
            {
                var passportData = new PassportData
                {
                    FullName = "John Smith",
                    DateOfBirth = "15-05-1985",
                    PassportNumber = "AB123456"
                };
                return Ok(passportData);
            }
            else if (request.DocumentType.ToLower() == "vehicle")
            {
                var vehicleData = new VehicleData
                {
                    VehicleMake = "Toyota",
                    VehicleModel = "Camry",
                    VehicleYear = "2020",
                    VehiclePlateNumber = "XYZ789"
                };
                return Ok(vehicleData);
            }
            
            return BadRequest("Invalid document type");
        }

        [HttpPost("generatePolicy")]
        public async Task<IActionResult> GeneratePolicy([FromBody] InsuranceData data)
        {
            if (data == null)
            {
                return BadRequest("Document data is required");
            }

            var policy = await _insuranceService.GeneratePolicyAsync(data);
            return Ok(policy);
        }
        
        [HttpGet("active-sessions")]
        public IActionResult GetActiveSessions()
        {
            // This endpoint could be used to monitor active bot sessions
            return Ok(new { ActiveSessions = "This would show information about active user sessions" });
        }
}

public class DocumentExtractionRequest
{
    public string DocumentType { get; set; } 
    public string DocumentImageBase64 { get; set; } 
    public long? ChatId { get; set; } 
}