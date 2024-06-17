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

await connection.StartAsync();

Console.WriteLine("Connected to Runner");

var botService = new BotService();
connection.On<Guid>("Registered",
    (id) =>
    {
        Console.WriteLine($"Registered: {id}");
        BotService.SetBotId(id);
    });

connection.On<string>(
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

        Console.WriteLine("========");
        Console.WriteLine($"Game Tick: {botState.GameTick}");
    }
);

connection.On<string>(
    "ReceiveGameComplete",
    (state) =>
    {
        Console.WriteLine($"Game Complete : {state}");
    });

connection.On<string>(
    "EndGame",
    (state) =>
    {
        Console.WriteLine($"End Game: {state}");
    });

connection.Closed += (error) =>
{
    Console.WriteLine($"Server closed with error: {error?.Message}");
    return Task.CompletedTask;
};

await connection.InvokeAsync("Register", token, botNickname);

while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
{
    if (botService.HasReceivedBotState() && connection.State == HubConnectionState.Connected)
    {
        BotCommand command = botService.ProcessState();

        //Console.WriteLine(botService.PrintBotView());
        await connection.InvokeAsync("SendPlayerCommand", command);
    }
}

await connection.StopAsync();
