using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SproutReferenceBot.Models;
using SproutReferenceBot.Services;

var builder = new ConfigurationBuilder().AddJsonFile(
    $"appsettings.json",
    optional: false
);

var configuration = builder.Build();
var environmentIp = Environment.GetEnvironmentVariable("RUNNER_IPV4");
var ip = !string.IsNullOrWhiteSpace(environmentIp)
    ? environmentIp
    : configuration.GetSection("RunnerIP").Value;
ip = ip != null && ip.StartsWith("http://") ? ip : "http://" + ip;

var botNickname =
    Environment.GetEnvironmentVariable("BOT_NICKNAME")
    ?? configuration.GetSection("BotNickname").Value;

var token =
    Environment.GetEnvironmentVariable("Token") ??
    Environment.GetEnvironmentVariable("REGISTRATION_TOKEN");

var port = configuration.GetSection("RunnerPort");

var url = ip + ":" + port.Value + "/runnerhub";

var connection = new HubConnectionBuilder()
    .WithUrl($"{url}")
    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug); })
    .WithAutomaticReconnect()
    .Build();

_ = connection.StartAsync();

Console.WriteLine("Connected to Runner");

var botService = new BotService();
connection.On<Guid>("Registered",
    (id) =>
    {
        Console.WriteLine($"Registered: {id}");
        botService.SetBotId(id);
    });

connection.On<String>(
    "Disconnect",
    (reason) =>
    {
        Console.WriteLine($"Server sent disconnect with reason: {reason}");
        _ = connection.StopAsync();
    }
);

connection.On<BotStateDTO>(
    "ReceiveBotState",
    (botState) =>
    {
        botService.SetBotState(botState);
        Console.WriteLine("ReceiveBotState");
    }
);

connection.On<string>(
    "ReceiveGameComplete",
    (state) =>
    {
        Console.WriteLine($"Game Complete : {state}");
    });


connection.On<object>(
    "EndGame",
    (state) =>
    {
        Console.WriteLine($"End Game: {state}");
    });

connection.Closed += (error) =>
{
    Console.WriteLine($"Server closed with error: {error}");
    return Task.CompletedTask;
};

_ = connection.InvokeAsync("Register", token, botNickname);

while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
{
    await Task.Delay(200);

    if (botService.HasReceivedBotState() && connection.State == HubConnectionState.Connected)
    {
        var state = botService.GetBotState();

        BotCommand command = botService.ProcessState();
        _ = connection.InvokeAsync("SendPlayerCommand", command);
        Console.WriteLine($"Sending Player Command: ({state?.X}, {state?.Y}), {command.Action}");
    }
}

_ = connection.StopAsync();
