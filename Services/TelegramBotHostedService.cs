namespace CarInsuranceSalesBot.Services;

public class TelegramBotHostedService : IHostedService
{
    private readonly TelegramBotService _botService;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(TelegramBotService botService, ILogger<TelegramBotHostedService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram Bot Service");
        await _botService.StartBotAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram Bot Service");
        await _botService.StopBotAsync(cancellationToken);
    }
}