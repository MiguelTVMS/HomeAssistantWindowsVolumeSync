using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class HomeAssistantClientTests
{
    private readonly Mock<ILogger<HomeAssistantClient>> _loggerMock;
    private readonly IConfiguration _configuration;
    private const string TestWebhookUrl = "https://test-ha.local/api/webhook/test_webhook";

    public HomeAssistantClientTests()
    {
        _loggerMock = new Mock<ILogger<HomeAssistantClient>>();
        
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", TestWebhookUrl }
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithValidUrl_SendsCorrectPayload()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedContent = null;
        string? capturedUrl = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                capturedUrl = request.RequestUri?.ToString();
                capturedContent = await request.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert
        Assert.Equal(TestWebhookUrl, capturedUrl);
        Assert.NotNull(capturedContent);
        
        var payload = JsonDocument.Parse(capturedContent);
        Assert.Equal(50, payload.RootElement.GetProperty("volume").GetInt32());
        Assert.False(payload.RootElement.GetProperty("mute").GetBoolean());
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithMuted_SendsCorrectMuteState()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedContent = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                capturedContent = await request.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        await client.SendVolumeUpdateAsync(0, true);

        // Assert
        Assert.NotNull(capturedContent);
        
        var payload = JsonDocument.Parse(capturedContent);
        Assert.Equal(0, payload.RootElement.GetProperty("volume").GetInt32());
        Assert.True(payload.RootElement.GetProperty("mute").GetBoolean());
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithNoUrl_DoesNotSendRequest()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var handlerMock = new Mock<HttpMessageHandler>();
        var requestMade = false;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => requestMade = true)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, emptyConfig);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert
        Assert.False(requestMade);
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithHttpError_LogsWarning()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert - verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("InternalServerError")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithException_LogsError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert - verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(50, false)]
    [InlineData(100, false)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    public async Task SendVolumeUpdateAsync_WithVariousVolumes_SendsCorrectValues(int volume, bool muted)
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedContent = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                capturedContent = await request.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        await client.SendVolumeUpdateAsync(volume, muted);

        // Assert
        Assert.NotNull(capturedContent);
        
        var payload = JsonDocument.Parse(capturedContent);
        Assert.Equal(volume, payload.RootElement.GetProperty("volume").GetInt32());
        Assert.Equal(muted, payload.RootElement.GetProperty("mute").GetBoolean());
    }
}
