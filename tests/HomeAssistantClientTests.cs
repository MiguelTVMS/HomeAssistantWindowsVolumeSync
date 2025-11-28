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
    private readonly IAppConfiguration _configuration;
    private const string TestWebhookUrl = "https://test-ha.local/api/webhook/test_webhook";
    private const string TestBaseUrl = "https://test-ha.local";
    private const string TestWebhookId = "test_webhook";

    public HomeAssistantClientTests()
    {
        _loggerMock = new Mock<ILogger<HomeAssistantClient>>();

        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", TestBaseUrl },
            { "HomeAssistant:WebhookPath", "/api/webhook/" },
            { "HomeAssistant:WebhookId", TestWebhookId }
        };

        _configuration = CreateAppConfiguration(configData);
    }

    private static IAppConfiguration CreateAppConfiguration(Dictionary<string, string?> configData)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new AppConfiguration(config);
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
        var emptyConfig = CreateAppConfiguration(new Dictionary<string, string?> { });//new ConfigurationBuilder()

        ; //

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

    [Fact]
    public async Task SendVolumeUpdateAsync_WithTimeout_LogsWarning()
    {
        // Arrange
        var configWithPlayer = CreateAppConfiguration(new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", TestBaseUrl }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", TestWebhookId },
            { "HomeAssistant:TargetMediaPlayer", "media_player.test" }
        });

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, configWithPlayer);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert - verify warning was logged (timeout warning)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("timed out")),
                It.IsAny<TaskCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithTargetMediaPlayer_IncludesInPayload()
    {
        // Arrange
        var configWithPlayer = CreateAppConfiguration(new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", TestBaseUrl }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", TestWebhookId },
            { "HomeAssistant:TargetMediaPlayer", "media_player.test_speaker" }
        });

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
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, configWithPlayer);

        // Act
        await client.SendVolumeUpdateAsync(75, false);

        // Assert
        Assert.NotNull(capturedContent);

        var payload = JsonDocument.Parse(capturedContent);
        Assert.Equal("media_player.test_speaker", payload.RootElement.GetProperty("targetMediaPlayer").GetString());
    }

    [Fact]
    public void Constructor_WithMissingWebhookUrl_LogsWarning()
    {
        // Arrange
        var emptyConfig = CreateAppConfiguration(new Dictionary<string, string?> { });//new ConfigurationBuilder()

        ; //

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, emptyConfig);

        // Assert - verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("webhook URL is not configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithMissingTargetMediaPlayer_LogsWarning()
    {
        // Arrange
        var configWithoutPlayer = CreateAppConfiguration(new Dictionary<string, string?>
            {
                { "HomeAssistant:WebhookUrl", TestWebhookUrl }
            });

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, configWithoutPlayer);

        // Assert - verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Target media player is not configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithHttpUrl_LogsWarning()
    {
        // Arrange
        var httpConfig = CreateAppConfiguration(new Dictionary<string, string?>
            {
                { "HomeAssistant:WebhookUrl", "http://test-ha.local" }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", "test_webhook" },
                { "HomeAssistant:TargetMediaPlayer", "media_player.test" }
            });

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, httpConfig);

        // Assert - verify warning was logged about HTTP
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("HTTP") && o.ToString()!.Contains("unencrypted")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithHttpsAndStrictTlsFalse_LogsWarning()
    {
        // Arrange
        var httpsConfigNoStrictTls = CreateAppConfiguration(new Dictionary<string, string?>
            {
                { "HomeAssistant:WebhookUrl", "https://test-ha.local" }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", "test_webhook" },
                { "HomeAssistant:TargetMediaPlayer", "media_player.test" },
                { "HomeAssistant:StrictTLS", "false" }
            });

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, httpsConfigNoStrictTls);

        // Assert - verify warning was logged about disabled certificate validation
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("certificate validation disabled") || o.ToString()!.Contains("StrictTLS=false")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithHttpsAndStrictTlsTrue_NoWarningAboutTls()
    {
        // Arrange
        var httpsConfigStrictTls = CreateAppConfiguration(new Dictionary<string, string?>
            {
                { "HomeAssistant:WebhookUrl", "https://test-ha.local" }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", "test_webhook" },
                { "HomeAssistant:TargetMediaPlayer", "media_player.test" },
                { "HomeAssistant:StrictTLS", "true" }
            });

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, httpsConfigStrictTls);

        // Assert - verify NO warning was logged about TLS (only checks for TLS-related warnings, not missing config warnings)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("certificate") || o.ToString()!.Contains("StrictTLS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_WithHttpsAndDefaultStrictTls_NoWarningAboutTls()
    {
        // Arrange
        var httpsConfigDefaultStrictTls = CreateAppConfiguration(new Dictionary<string, string?>
            {
                { "HomeAssistant:WebhookUrl", "https://test-ha.local" }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", "test_webhook" },
                { "HomeAssistant:TargetMediaPlayer", "media_player.test" }
                // StrictTLS not specified, should default to true
            });

        var httpClient = new HttpClient();

        // Act
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, httpsConfigDefaultStrictTls);

        // Assert - verify NO warning was logged about TLS
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("certificate") || o.ToString()!.Contains("StrictTLS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVolumeUpdateAsync_WithHttpUrl_SendsSuccessfully()
    {
        // Arrange
        var httpConfig = CreateAppConfiguration(new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "http://test-ha.local" }, { "HomeAssistant:WebhookPath", "/api/webhook/" }, { "HomeAssistant:WebhookId", "test_webhook" },
            { "HomeAssistant:TargetMediaPlayer", "media_player.test" }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedUrl = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedUrl = request.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, httpConfig);

        // Act
        await client.SendVolumeUpdateAsync(50, false);

        // Assert
        Assert.Equal("http://test-ha.local/api/webhook/test_webhook", capturedUrl);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task CheckHealthAsync_With2xxStatusCodes_ReturnsTrue(HttpStatusCode statusCode)
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task CheckHealthAsync_WithNon2xxStatusCodes_ReturnsFalse(HttpStatusCode statusCode)
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNetworkError_ReturnsFalse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeout_ReturnsFalse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new HomeAssistantClient(httpClient, _loggerMock.Object, _configuration);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }
}

