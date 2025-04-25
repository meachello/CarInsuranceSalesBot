using CarInsuranceSalesBot.Models;

namespace CarInsuranceSalesBot.Services;

public class MindeeService
{
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(ILogger<MindeeService> logger)
    {
        _logger = logger;
    }

    public async Task<PassportData> ExtractPassportDataAsync(string photoFileId)
    {
        _logger.LogInformation("Extracting passport data from photo {PhotoId}", photoFileId);
            
        // Simulate API delay
        await Task.Delay(1000);
            
        // Mock passport data
        return new PassportData
        {
            FullName = "John Smith",
            DateOfBirth = "15-05-1985",
            PassportNumber = "AB123456"
        };
    }

    public async Task<VehicleData> ExtractVehicleDataAsync(string photoFileId)
    {
        _logger.LogInformation("Extracting vehicle data from photo {PhotoId}", photoFileId);
            
        // Simulate API delay
        await Task.Delay(1000);
            
        // Mock vehicle data
        return new VehicleData
        {
            VehicleMake = "Toyota",
            VehicleModel = "Camry",
            VehicleYear = "2020",
            VehiclePlateNumber = "XYZ789"
        };
    }
}