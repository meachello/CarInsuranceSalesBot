using CarInsuranceSalesBot.Models;

namespace CarInsuranceSalesBot.Services;

public class InsuranceService
{
    private readonly GeminiService _geminiService;
        private readonly ILogger<InsuranceService> _logger;

        public InsuranceService(GeminiService geminiService, ILogger<InsuranceService> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<PolicyDocument> GeneratePolicyAsync(InsuranceData data)
        {
            _logger.LogInformation("Generating policy for {FullName}", data.FullName);
            
            // Generate policy using Gemini 
            string policyPrompt = $"Generate a car insurance policy without any text input brackets, just general text for {data.FullName} for a {data.VehicleYear} " +
                                 $"{data.VehicleMake} {data.VehicleModel} with license plate {data.VehiclePlateNumber}. " +
                                 $"The policy costs 100 USD and is valid for one year from today.";
            
            string policyText = await _geminiService.GenerateResponseAsync(policyPrompt);
            
            if (string.IsNullOrEmpty(policyText))
            {
                policyText = $"CAR INSURANCE POLICY\n\n" +
                            $"POLICY NUMBER: POL-{DateTime.Now.ToString("yyyyMMdd")}-{new Random().Next(1000, 9999)}\n\n" +
                            $"INSURED: {data.FullName}\n" +
                            $"VEHICLE: {data.VehicleYear} {data.VehicleMake} {data.VehicleModel}\n" +
                            $"LICENSE PLATE: {data.VehiclePlateNumber}\n" +
                            $"COVERAGE PERIOD: {DateTime.Now.ToString("dd-MM-yyyy")} to {DateTime.Now.AddYears(1).ToString("dd-MM-yyyy")}\n" +
                            $"PREMIUM: 100 USD\n\n" +
                            $"This policy provides standard coverage including liability, collision, and comprehensive insurance " +
                            $"as per the terms and conditions of our standard insurance agreement.";
            }
            
            return new PolicyDocument
            {
                PolicyNumber = $"POL-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                FullName = data.FullName,
                VehicleInfo = $"{data.VehicleYear} {data.VehicleMake} {data.VehicleModel}",
                LicensePlate = data.VehiclePlateNumber,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddYears(1),
                Premium = 100,
                PolicyText = policyText
            };
        }
}

public class PolicyDocument
{
    public string PolicyNumber { get; set; }
    public string FullName { get; set; }
    public string VehicleInfo { get; set; }
    public string LicensePlate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Premium { get; set; }
    public string PolicyText { get; set; }
}