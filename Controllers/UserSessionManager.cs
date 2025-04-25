using CarInsuranceSalesBot.Models;

namespace CarInsuranceSalesBot.Controllers;

public class UserSessionManager
{
    private readonly Dictionary<long, UserState> _userStates = new();
    private readonly Dictionary<long, string> _passportPhotoIds = new();
    private readonly Dictionary<long, string> _vehiclePhotoIds = new();
    private readonly Dictionary<long, InsuranceData> _extractedData = new();

    public UserState GetUserState(long chatId)
    {
        if (_userStates.TryGetValue(chatId, out UserState state))
        {
            return state;
        }
        return UserState.New;
    }

    public void SetUserState(long chatId, UserState state)
    {
        _userStates[chatId] = state;
    }

    public void SetPassportPhotoId(long chatId, string photoId)
    {
        _passportPhotoIds[chatId] = photoId;
    }

    public string GetPassportPhotoId(long chatId)
    {
        return _passportPhotoIds.TryGetValue(chatId, out var photoId) ? photoId : null;
    }

    public void SetVehiclePhotoId(long chatId, string photoId)
    {
        _vehiclePhotoIds[chatId] = photoId;
    }

    public string GetVehiclePhotoId(long chatId)
    {
        return _vehiclePhotoIds.TryGetValue(chatId, out var photoId) ? photoId : null;
    }

    public void SetExtractedData(long chatId, InsuranceData data)
    {
        _extractedData[chatId] = data;
    }

    public InsuranceData GetExtractedData(long chatId)
    {
        return _extractedData.TryGetValue(chatId, out var data) ? data : null;
    }
}