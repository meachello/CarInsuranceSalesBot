using CarInsuranceSalesBot.Controllers;
using CarInsuranceSalesBot.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CarInsuranceSalesBot.Services;

public class TelegramBotService
{
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly UserSessionManager _sessionManager;
        private readonly GeminiService _geminiService;
        private readonly MindeeService _mindeeService;
        private readonly InsuranceService _insuranceService;
        private CancellationTokenSource _cts;

        public TelegramBotService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotService> logger,
            UserSessionManager sessionManager,
            GeminiService geminiService,
            MindeeService mindeeService,
            InsuranceService insuranceService)
        {
            _botClient = botClient;
            _logger = logger;
            _sessionManager = sessionManager;
            _geminiService = geminiService;
            _mindeeService = mindeeService;
            _insuranceService = insuranceService;
        }

        public async Task StartBotAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );

            var me = await _botClient.GetMe(cancellationToken);
            _logger.LogInformation("Telegram bot started: {BotName}", me.Username);
        }

        public async Task StopBotAsync(CancellationToken cancellationToken)
        {
            // Send cancellation request to stop bot
            _cts?.Cancel();
            await Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                // Process different types of updates
                if (update.Message is not null)
                {
                    await ProcessMessageAsync(update.Message, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
            }
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            _logger.LogInformation("Processing message from chat {ChatId}", chatId);

            try
            {
                // Check if this is a text message
                if (message.Text is not null)
                {
                    var text = message.Text.ToLower();
                    
                    // Check if this is the first message (start command)
                    if (text == "/start")
                    {
                        await SendWelcomeMessageAsync(chatId, cancellationToken);
                        await RequestDocumentsAsync(chatId, cancellationToken);
                        _sessionManager.SetUserState(chatId, UserState.WaitingForPassport);
                        return;
                    }

                    // Process text based on current state
                    await ProcessTextBasedOnStateAsync(chatId, text, cancellationToken);
                }
                // Check if this is a photo
                else if (message.Photo is not null && message.Photo.Length > 0)
                {
                    await ProcessPhotoBasedOnStateAsync(chatId, message.Photo, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "I can only process text messages and photos. Please try again.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for chat {ChatId}", chatId);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Sorry, there was an error processing your request. Please try again.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ProcessTextBasedOnStateAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            var currentState = _sessionManager.GetUserState(chatId);
            
            switch (currentState)
            {
                case UserState.ConfirmingDocumentData:
                    if (text.Contains("yes") || text.Contains("correct") || text.Contains("confirm"))
                    {
                        await ConfirmPriceAsync(chatId, cancellationToken);
                        _sessionManager.SetUserState(chatId, UserState.WaitingForPriceConfirmation);
                    }
                    else if (text.Contains("no") || text.Contains("incorrect") || text.Contains("wrong"))
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "I'm sorry for the inaccuracy. Please send clear photos of your documents again, starting with your passport.",
                            cancellationToken: cancellationToken);
                        _sessionManager.SetUserState(chatId, UserState.WaitingForPassport);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Please confirm if the extracted data is correct by typing 'yes' or 'no'.",
                            cancellationToken: cancellationToken);
                    }
                    break;
                
                case UserState.WaitingForPriceConfirmation:
                    if (text.Contains("yes") || text.Contains("agree") || text.Contains("accept"))
                    {
                        await GenerateAndSendPolicyAsync(chatId, cancellationToken);
                        _sessionManager.SetUserState(chatId, UserState.Completed);
                    }
                    else if (text.Contains("no") || text.Contains("disagree") || text.Contains("decline"))
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "I apologize, but 100 USD is our fixed price for car insurance. " +
                                 "Would you like to proceed with this price?",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Please indicate if you agree with the price by typing 'yes' or 'no'.",
                            cancellationToken: cancellationToken);
                    }
                    break;
                
                case UserState.Completed:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Your insurance policy has already been issued. If you'd like to purchase another policy, please type /start to begin a new process.",
                        cancellationToken: cancellationToken);
                    break;
                
                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "I'm not sure what you're asking for. Please follow the instructions or type /start to begin the car insurance purchase process.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task ProcessPhotoBasedOnStateAsync(long chatId, PhotoSize[] photos, CancellationToken cancellationToken)
        {
            var currentState = _sessionManager.GetUserState(chatId);
            
            switch (currentState)
            {
                case UserState.WaitingForPassport:
                    // Save photo info for later processing
                    var passportPhotoId = photos[^1].FileId; // Get the highest quality photo
                    _sessionManager.SetPassportPhotoId(chatId, passportPhotoId);
                    
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Thank you for sending your passport photo. Now, please send a photo of your vehicle identification document.",
                        cancellationToken: cancellationToken);
                    _sessionManager.SetUserState(chatId, UserState.WaitingForVehicleDocument);
                    break;
                
                case UserState.WaitingForVehicleDocument:
                    // Save vehicle document photo info
                    var vehiclePhotoId = photos[^1].FileId;
                    _sessionManager.SetVehiclePhotoId(chatId, vehiclePhotoId);
                    
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Thank you for submitting both documents. I'm now processing them...",
                        cancellationToken: cancellationToken);
                    
                    // Process documents using Mindee API (mock)
                    await ProcessDocumentsAsync(chatId, cancellationToken);
                    _sessionManager.SetUserState(chatId, UserState.ConfirmingDocumentData);
                    break;
                
                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "I'm not expecting a photo at this stage. Please follow the instructions.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task SendWelcomeMessageAsync(long chatId, CancellationToken cancellationToken)
        {
            string welcomeMessage = await _geminiService.GenerateResponseAsync("Generate a friendly welcome message for a car insurance Telegram bot without any text input brackets, just general text");
            
            if (string.IsNullOrEmpty(welcomeMessage))
            {
                welcomeMessage = "👋 Welcome to our Car Insurance Bot! I'm here to help you quickly purchase car insurance. " +
                                "I'll guide you through document submission, verification, and policy issuance.";
            }
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                cancellationToken: cancellationToken);
        }
        
        private async Task RequestDocumentsAsync(long chatId, CancellationToken cancellationToken)
        {
            string documentRequest = await _geminiService.GenerateResponseAsync("Generate a message asking the user to submit a passport photo for car insurance without any text input brackets, just general text");
            
            if (string.IsNullOrEmpty(documentRequest))
            {
                documentRequest = "To get started, please send me a clear photo of your passport. " +
                                 "Make sure all text is readable and the entire document is visible.";
            }
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: documentRequest,
                cancellationToken: cancellationToken);
        }
        
        private async Task ProcessDocumentsAsync(long chatId, CancellationToken cancellationToken)
        {
            // Get photo file IDs from session
            var passportPhotoId = _sessionManager.GetPassportPhotoId(chatId);
            var vehiclePhotoId = _sessionManager.GetVehiclePhotoId(chatId);
            
            if (string.IsNullOrEmpty(passportPhotoId) || string.IsNullOrEmpty(vehiclePhotoId))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "There was an issue with your documents. Please start over by typing /start.",
                    cancellationToken: cancellationToken);
                return;
            }
            
            // Process passport photo with Mindee API (mock)
            var passportData = await _mindeeService.ExtractPassportDataAsync(passportPhotoId);
            
            // Process vehicle document photo with Mindee API (mock)
            var vehicleData = await _mindeeService.ExtractVehicleDataAsync(vehiclePhotoId);
            
            // Combine extracted data
            var extractedData = new InsuranceData()
            {
                FullName = passportData.FullName,
                DateOfBirth = passportData.DateOfBirth,
                PassportNumber = passportData.PassportNumber,
                VehicleMake = vehicleData.VehicleMake,
                VehicleModel = vehicleData.VehicleModel,
                VehicleYear = vehicleData.VehicleYear,
                VehiclePlateNumber = vehicleData.VehiclePlateNumber
            };
            
            // Store extracted data in session
            _sessionManager.SetExtractedData(chatId, extractedData);
            
            // Present data to user for confirmation
            string confirmationMessage = $"I've extracted the following information from your documents:\n\n" +
                                       $"👤 Full Name: {extractedData.FullName}\n" +
                                       $"🎂 Date of Birth: {extractedData.DateOfBirth}\n" +
                                       $"🆔 Passport Number: {extractedData.PassportNumber}\n" +
                                       $"🚗 Vehicle: {extractedData.VehicleMake} {extractedData.VehicleModel} ({extractedData.VehicleYear})\n" +
                                       $"🔢 License Plate: {extractedData.VehiclePlateNumber}\n\n" +
                                       $"Is this information correct? Please reply with 'yes' or 'no'.";
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: confirmationMessage,
                cancellationToken: cancellationToken);
        }
        
        private async Task ConfirmPriceAsync(long chatId, CancellationToken cancellationToken)
        {
            string priceMessage = await _geminiService.GenerateResponseAsync("Generate a short message informing the user that car insurance costs 100 USD and asking for confirmation without any text input brackets, just general text");
            
            if (string.IsNullOrEmpty(priceMessage))
            {
                priceMessage = "Based on the information provided, your car insurance premium is 100 USD. " +
                              "Do you agree with this price and wish to proceed with the purchase?";
            }
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: priceMessage,
                cancellationToken: cancellationToken);
        }
        
        private async Task GenerateAndSendPolicyAsync(long chatId, CancellationToken cancellationToken)
        {
            var extractedData = _sessionManager.GetExtractedData(chatId);
            
            if (extractedData == null)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "There was an issue retrieving your data. Please start over by typing /start.",
                    cancellationToken: cancellationToken);
                return;
            }
            
            // Generate insurance policy
            var policy = await _insuranceService.GeneratePolicyAsync(extractedData);
            
            // Save policy to file
            string fileName = $"policy_{chatId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            await File.WriteAllTextAsync(fileName, policy.PolicyText, cancellationToken);
            
            // Send confirmation message
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Thank you for your purchase! Your insurance policy has been generated successfully. Here is your policy document:",
                cancellationToken: cancellationToken);
            
            // Send policy file
            await using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            await _botClient.SendDocument(
                chatId: chatId,
                document: InputFile.FromStream(fileStream, fileName),
                caption: "Your Car Insurance Policy",
                cancellationToken: cancellationToken);
            
            // Send completion message
            string completionMessage = await _geminiService.GenerateResponseAsync("Generate a short thank you message for completing car insurance purchase without any text input brackets, just general text");
            
            if (string.IsNullOrEmpty(completionMessage))
            {
                completionMessage = "Thank you for choosing our insurance services! Your policy is now active. " +
                                   "If you have any questions or need assistance, feel free to contact our support team. Drive safely!";
            }
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: completionMessage,
                cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError("Telegram polling error: {ErrorMessage}", errorMessage);
            return Task.CompletedTask;
        }
}