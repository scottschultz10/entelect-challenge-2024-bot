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
    async (reason) =>
    {
        Console.WriteLine($"Server sent disconnect with reason: {reason}");
        await connection.StopAsync();
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


connection.On<Guid>(
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

Console.WriteLine($"Token: {token}");
Console.WriteLine($"BotNickname: {botNickname}");
Console.WriteLine($"Connection ID: {connection.ConnectionId}");
Console.WriteLine($"Connection state: {connection.State}");
Console.WriteLine($"URL: {url}");
Console.WriteLine($"environmentIp: {environmentIp}");

_ = connection.InvokeAsync("Register", token, botNickname);

while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
{
    await Task.Delay(100);

    var state = botService.GetBotState();
    var botId = botService.GetBotId();

    Console.WriteLine($"In while bot state = {state == null} / connection state {connection.State} / has received state{botService.HasReceivedBotState()}");

    if (state == null)
    {
        continue;
    }

    if (botService.HasReceivedBotState())
    {
        _ = connection.InvokeAsync("SendPlayerCommand", botService.ProcessState());
    }

    Console.WriteLine($"In while connected: {botId}, ({state.X}, {state.Y})");
}

_ = connection.StopAsync();