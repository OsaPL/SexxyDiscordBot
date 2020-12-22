using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Sexxy_Discord_Bot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private const string _usersFile = "allowedToUseBot";

        private string _token;
        private string _masterId;

        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
        {
            new Program().MainAsync(args).GetAwaiter().GetResult();
        }

        public Program()
        {
            // It is recommended to Dispose of a client when you are finished
            // using it, at the end of your app's lifetime.
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task MainAsync(string[] strings)
        {
            _token = strings[0];
            _masterId = strings[1];

            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            try
            {
                //TODO! This 'if' for DMs only DOESNT WORK! WHY?
                if (await message.Channel.GetUsersAsync().CountAsync() <= 1)
                {
                    if (message.Content.First() == '!')
                    {
                        if (await CheckIfAllowed(message.Author))
                        {
                            await ParseMessage(message);
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync(
                                $"You're not allowed to use me. Id:`{message.Author.Id}`");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await message.Channel.SendMessageAsync(
                    $"Woops, I pooped an error, send this to Osa:```cs\r\n{e.ToString()}```");
                throw;
            }
        }

        private async Task<bool> CheckIfAllowed(SocketUser author)
        {
            if (!File.Exists(_usersFile))
            {
                File.Create(_usersFile);
            }
            
            await LogAsync(new LogMessage(LogSeverity.Info, "CheckIfAllowed",
                $"Checking if {author.Username}#{author.Id} can even use commands."));
            // The bot should never respond to itself.
            if (author.Id == _client.CurrentUser.Id)
                return false;

            if (author.Id.ToString() == _masterId)
            {
                return true;
            }

            var users = File.ReadLines(_usersFile);
            if (users.Contains(author.Id.ToString()))
            {
                return true;
            }

            return false;
        }

        private async Task ParseMessage(SocketMessage socketMessage)
        {
            var parts = socketMessage.Content.Split(' ');
            var command = parts[0];
            string[] arguments = parts.Skip(1).ToArray();
            switch (command)
            {
                case "!addUser":
                    await AddUser(arguments);
                    break;
                case "!removeUser":
                    await RemoveUser(arguments);
                    break;
                case "!restartServer":
                    await RestartServer(arguments);
                    break;
                default:
                    await socketMessage.Channel.SendMessageAsync("Unrecognized command!");
                    break;
            }
        }

        private async Task RestartServer(string[] arguments)
        {
            switch (arguments[0])
            {
                case "dst":
                    await ExecuteCommand("sudo systemctl restart dstServer.service");
                    break;
                case "starbound":
                    await ExecuteCommand("sudo systemctl restart starboundServer.service");
                    break;
                case "mc":
                    await ExecuteCommand("sudo systemctl restart mcServer.service");
                    break;
            }
        }

        private async Task ExecuteCommand(string command)
        {
            string result = "";
            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.FileName = "/bin/bash";
                proc.StartInfo.Arguments = "-c \" " + command + " \"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();

                result += proc.StandardOutput.ReadToEnd();
                result += proc.StandardError.ReadToEnd();

                proc.WaitForExit();
            }
        }

        private async Task RemoveUser(string[] arguments)
        {
            var users = File.ReadLines(_usersFile);
            var newUsers = users.Where(x => x != arguments[0]);
            await File.WriteAllLinesAsync(_usersFile, newUsers);
        }

        private async Task AddUser(string[] arguments)
        {
            await File.AppendAllLinesAsync(_usersFile, new[] {arguments[0]});
        }
    }
}