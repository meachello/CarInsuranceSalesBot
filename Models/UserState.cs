namespace CarInsuranceSalesBot.Models;

public enum UserState
{
    New,
    WaitingForPassport,
    WaitingForVehicleDocument,
    ConfirmingDocumentData,
    WaitingForPriceConfirmation,
    Completed
}