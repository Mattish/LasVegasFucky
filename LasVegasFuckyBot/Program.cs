using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace LasVegasFuckyBot
{
    enum FuckyStatus
    {
        NotFucky = 0,
        Fucky = 1
    }

    class FuckyState
    {
        public DateTimeOffset Timestamp;
        public FuckyStatus Status;
        public string Message;
        public string Username;
    }

    public static class Program
    {
        private static readonly string fileName = "Fucky_State.json";

        public static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => cancellationTokenSource.Cancel();

            var client = new DiscordSocketClient();

            client.Log += LogAsync;
            client.Ready += Ready;
            client.MessageReceived += MessageReceivedAsync;
            await client.LoginAsync(TokenType.Bot, args[0]);
            await client.StartAsync();

            await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith((_) => { });
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private static Task Ready()
        {
            Console.WriteLine("Connected!");

            return Task.CompletedTask;
        }

        private static async Task MessageReceivedAsync(SocketMessage message)
        {
            FileGuard();

            var json = File.ReadAllText(fileName);
            var fuckyState = JsonConvert.DeserializeObject<FuckyState>(json);

            if (message.Content.ToLower() == "!status")
            {
                var statusMessage = await GetStatusMessage(fuckyState, false);
                await message.Channel.SendMessageAsync(statusMessage);
            }
            else if (message.Content.ToLower().StartsWith("!fucky "))
            {
                fuckyState = new FuckyState
                {
                    Status = FuckyStatus.Fucky,
                    Message = message.Content.Substring(7).Replace(Environment.NewLine, string.Empty),
                    Timestamp = DateTimeOffset.UtcNow,
                    Username = message.Author.Username
                };
                json = JsonConvert.SerializeObject(fuckyState);
                File.WriteAllText(fileName, json);
                var statusMessage = await GetStatusMessage(fuckyState, true);
                await message.Channel.SendMessageAsync(statusMessage);
            }
            else if (message.Content.ToLower() == "!unfucky")
            {
                fuckyState = new FuckyState
                {
                    Status = FuckyStatus.NotFucky,
                    Timestamp = DateTimeOffset.UtcNow,
                    Username = message.Author.Username
                };
                json = JsonConvert.SerializeObject(fuckyState);
                File.WriteAllText(fileName, json);
                var statusMessage = await GetStatusMessage(fuckyState, true);
                await message.Channel.SendMessageAsync(statusMessage);
            }
        }

        private static void FileGuard()
        {
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(new FuckyState
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Status = FuckyStatus.NotFucky,
                    Username = "BotInitial"
                }));
            }
        }

        private static Task<string> GetStatusMessage(FuckyState fuckyState, bool beingSet)
        {
            return Task.FromResult($"{(beingSet ? "Set to" : "Status is")} **{fuckyState.Status.ToString()}** set by *{fuckyState.Username}* on {fuckyState.Timestamp:F} {(string.IsNullOrEmpty(fuckyState.Message) ? "" : $"with message '{fuckyState.Message}'")}");
        }
    }
}
