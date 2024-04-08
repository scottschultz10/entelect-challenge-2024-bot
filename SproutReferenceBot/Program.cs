﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SproutReferenceBot.Models;
using SproutReferenceBot.Services;

string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
// Set up configuration sources.
var builder = new ConfigurationBuilder()
    .SetBasePath(executableDirectory + "../../../")
    .AddJsonFile(
    $"appsettings.json",
    optional: false);


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
connection.On<Guid>("Registered", (id) => { Console.WriteLine($"Registered: {id}"); botService.SetBotId(id); });

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
        //BotCommand command = botService.ProcessState(botState);
        Console.WriteLine($"Sending something random UP");
        connection.InvokeAsync("SendPlayerCommand", new BotCommand() { BotId = botService.GetBotId(), Action = SproutReferenceBot.Enums.BotAction.Up });
    }
);

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

await connection.InvokeAsync("Register", token, botNickname);

while (connection.State is HubConnectionState.Connected or HubConnectionState.Connecting)
{
    var state = botService.GetBotState();
    var botId = botService.GetBotId();
    if (state == null || botId == null)
    {
        continue;
    }
    Console.WriteLine($"In while connected: {botId}, ({state.X}, {state.Y})");
}