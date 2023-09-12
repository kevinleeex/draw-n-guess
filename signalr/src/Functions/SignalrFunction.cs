using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace Functions;

public class SignalrFunction
{
    private static readonly HttpClient HttpClient = new();
    private readonly ILogger _logger;
    private readonly ServiceHubContext serviceHubContext;
    
    private const string MyHubName = "dng";
    private const string OnlineGroupName = "online";
    
    // TODO: This is not a good way to store game status in case of multiple instances
    private static readonly ConcurrentDictionary<string, string> Status = new();
    
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
            drawDataDto = JsonSerializer.Deserialize<DrawData>(drawData);
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

    public class Message
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
        [JsonPropertyName("connectionId")] public string? ConnectionId { get; set; }
        [JsonPropertyName("system")] public bool SystemMessage { get; set; }
        [JsonPropertyName("own")] public bool Own { get; set; }
    }

    public class DrawData
    {
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; }
        [JsonPropertyName("x1")]
        public int X1 { get; set; }
        [JsonPropertyName("y1")]
        public int Y1 { get; set; }
        [JsonPropertyName("x2")]
        public int X2 { get; set; }
        [JsonPropertyName("y2")]
        public int Y2 { get; set; }
    }
    
    private string GetRandomUser()
    {
        var users = UserMapping.Values.ToList();
        var random = new Random();
        return users[random.Next(users.Count)];
    }
}