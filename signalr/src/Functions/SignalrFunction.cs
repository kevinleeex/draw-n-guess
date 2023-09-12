using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using signalr.Common;

namespace signalr.Functions;

public class SignalrFunction
{
    private readonly ILogger _logger;
    private readonly ServiceHubContext serviceHubContext;

    private const string MyHubName = "dng";

    private const string OnlineGroupName = "online";

    private static readonly HttpClient HttpClient = new();
    
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    // TODO: This is not a good way to store game status in case of multiple instances

    private static readonly GameStatus Status = new();

    // TODO: This is not a good way to store user mapping in case of multiple instances

    private static readonly Dictionary<string, string> UserMapping = new();

    public SignalrFunction(ILoggerFactory loggerFactory, ServiceManager serviceManager)
    {
        this.serviceHubContext = serviceManager.CreateHubContextAsync(MyHubName, default).GetAwaiter().GetResult();
        _logger = loggerFactory.CreateLogger<SignalrFunction>();
    }

    [Function("index")]
    public static HttpResponseData GetHomePage([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString(File.ReadAllText("content/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }

    [Function("negotiate")]
    public static HttpResponseData Negotiate([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = MyHubName)]
        string connectionInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(connectionInfo);
        return response;
    }

    [Function("OnConnected")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRGroupAction OnConnected(
        [SignalRTrigger(MyHubName, "connections", "connected")]
        SignalRInvocationContext invocationContext)
    {
        _logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        return new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = OnlineGroupName,
            ConnectionId = invocationContext.ConnectionId
        };
    }

    [Function("OnDisconnected")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRGroupAction OnDisconnected(
        [SignalRTrigger(MyHubName, "connections", "disconnected")] SignalRInvocationContext invocationContext)
    {
        _logger.LogInformation($"{invocationContext.ConnectionId} has disconnected");
        UserMapping.Remove(invocationContext.ConnectionId);
        return new SignalRGroupAction(SignalRGroupActionType.Remove)
        {
            GroupName = OnlineGroupName,
            ConnectionId = invocationContext.ConnectionId
        };
    }

    [Function("JoinChat")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRMessageAction JoinChat(
        [SignalRTrigger(MyHubName, "messages", "JoinChat", "user")]
        SignalRInvocationContext invocationContext,
        string user)
    {
        var message = new Message()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "System",
            Text = $"{user} has joined the chat",
            ConnectionId = invocationContext.ConnectionId,
            Timestamp = DateTime.UtcNow,
            SystemMessage = true
        };
        
        UserMapping.Add(invocationContext.ConnectionId, user);
        
        return new SignalRMessageAction("ReceivedMessage")
        {
            GroupName = OnlineGroupName,
            Arguments = new object[] {message}
        };
    }

    [Function("LeaveChat")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRMessageAction LeaveChat(
        [SignalRTrigger(MyHubName, "messages", "LeaveChat", "user")]
        SignalRInvocationContext invocationContext,
        string user)
    {
        var message = new Message()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "System",
            Text = $"{user} has left the chat",
            ConnectionId = invocationContext.ConnectionId,
            Timestamp = DateTime.UtcNow,
            SystemMessage = true
        };
        
        UserMapping.Remove(invocationContext.ConnectionId);

        return new SignalRMessageAction("ReceivedMessage")
        {
            GroupName = OnlineGroupName,
            Arguments = new object[] {message}
        };
    }
    
    [Function("SendMessage")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRMessageAction SendMessage(
        [SignalRTrigger(MyHubName, "messages", "SendMessage", "message")]
        SignalRInvocationContext invocationContext,
        string message)
    {
        var user = UserMapping[invocationContext.ConnectionId];
        var msg = new Message()
        {
            Id = Guid.NewGuid().ToString(),
            Name = user,
            Text = message,
            ConnectionId = invocationContext.ConnectionId,
            Timestamp = DateTime.UtcNow
        };

        // check if the message contains the word
        if (message.Contains(Status.Word))
        {
            var newMsg = msg.Text.Replace(Status.Word, new string('*', Status.Word.Length), StringComparison.InvariantCultureIgnoreCase);
            msg.Text = newMsg;
            if (invocationContext.ConnectionId != Status.Drawer)
            {
                serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("ReceivedMessage", new Message()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "System",
                    Text = $"{user} got the correct word!",
                    ConnectionId = invocationContext.ConnectionId,
                    Timestamp = DateTime.UtcNow,
                    SystemMessage = true
                });
            }
        }
        
        return new SignalRMessageAction("ReceivedMessage")
        {
            GroupName = OnlineGroupName,
            Arguments = new object[] {msg}
        };
    }


    [Function("SendDraw")]
    [SignalROutput(HubName = MyHubName)]
    public static SignalRMessageAction SendDraw(
        [SignalRTrigger(MyHubName, "messages", "SendDraw", "drawData")]
        SignalRInvocationContext invocationContext,
        string drawData)
    {
        DrawData? drawDataDto;
        try
        {
            drawDataDto = JsonSerializer.Deserialize<DrawData>(drawData, JsonOptions);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        if (drawDataDto == null)
        {
            throw new ArgumentException("Invalid draw data");
        }
        drawDataDto.ConnectionId = invocationContext.ConnectionId;
        return new SignalRMessageAction("ReceivedDraw", new object[] { drawDataDto });
    }
    
    [Function("GameControl")]
    [SignalROutput(HubName = MyHubName)]
    public SignalRMessageAction GameControl(
        [SignalRTrigger(MyHubName, "messages", "GameControl", "control")]
        SignalRInvocationContext invocationContext,
        string control)
    {
        switch (control)
        {
            case "RefreshWord":
            {
                if (invocationContext.ConnectionId == Status.Drawer)
                {
                    var newWord = Words.GetRandomWord();
                    Status.Word = newWord;
                    serviceHubContext.Clients.Client(Status.Drawer).SendAsync("RefreshGame", Status);
                    serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("ReceivedMessage", new Message()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "System",
                        Text = $"The drawer {UserMapping[Status.Drawer]} changed the word",
                        ConnectionId = invocationContext.ConnectionId,
                        Timestamp = DateTime.UtcNow,
                        SystemMessage = true
                    });
                }
                break;
            }
            case "RefreshGame":
            {
                RefreshGame();
                break;
            }
        }
        var user = UserMapping[invocationContext.ConnectionId];
        var msg = new Message()
        {
            Id = Guid.NewGuid().ToString(),
            Name = user,
            Text = control,
            ConnectionId = invocationContext.ConnectionId,
            Timestamp = DateTime.UtcNow
        };
        
        return new SignalRMessageAction("ReceivedMessage")
        {
            GroupName = OnlineGroupName,
            Arguments = new object[] {msg}
        };
    }
    
    [Function("GameSchedule")]
    public void GameSchedule(
        [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    {
        if (Status.Game == "started")
        {
            var endTime = DateTime.Parse(Status.Time);
            if (DateTime.Now < endTime) return;
            
            // round game is over, send message to all users
            serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("ReceivedMessage", new Message()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "System",
                Text = $"Time is up! The word is {Status.Word}. Game will restart in 5 seconds.",
                Timestamp = DateTime.UtcNow,
                SystemMessage = true
            });
            ResetGame();
        }
        else if (Status.Game == "waiting")
        {
            if (UserMapping.Count >= 2)
            {
               RefreshGame();
            }
            else
            {
                serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("ReceivedMessage", new Message()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "System",
                    Text = "Waiting for more players",
                    Timestamp = DateTime.UtcNow,
                    SystemMessage = true
                });
            }
        }
    }
    
    private void RefreshGame()
    {
        var drawer = GetRandomUser();

        serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("ReceivedMessage", new Message()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "System",
            Text = $"{UserMapping[drawer]} is drawing",
            Timestamp = DateTime.UtcNow,
            SystemMessage = true
        });
        
        var game = new GameStatus()
        {
            Drawer = drawer,
            Game = "started",
            Time = DateTime.UtcNow.AddSeconds(60).ToString("O")
        };
        serviceHubContext.Clients.GroupExcept(OnlineGroupName, drawer).SendAsync("RefreshGame", game);

        var newWord = Words.GetRandomWord();
        game.Word = newWord;
        
        Status.Drawer = game.Drawer;
        Status.Game = game.Game;
        Status.Time = game.Time;
        Status.Word = game.Word;
        
        serviceHubContext.Clients.Client(drawer).SendAsync("RefreshGame", game);
    }

    private void ResetGame()
    {
        var game = new GameStatus()
        {
            Drawer = "",
            Game = "waiting",
            Time = "",
            Word = ""
        };
        serviceHubContext.Clients.Group(OnlineGroupName).SendAsync("RefreshGame", game);
        Status.Drawer = game.Drawer;
        Status.Game = game.Game;
        Status.Time = game.Time;
        Status.Word = game.Word;
    }

    private string GetRandomUser()
    {
        var users = UserMapping.ToList();
        var random = new Random();
        return users[random.Next(users.Count)].Key;
    }
}

public class Message
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ConnectionId { get; set; }
    public bool SystemMessage { get; set; }
    public bool Own { get; set; }
}

public class DrawData
{
    public string ConnectionId { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}

public class GameStatus
{
    public string Word { get; set; } = "";
    public string Game { get; set; } = "waiting";
    public string Drawer { get; set; } = "";
    public string Time { get; set; } = "";
}