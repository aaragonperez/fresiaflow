using FresiaFlow.Adapters.Inbound.Api.Hubs;
using FresiaFlow.Adapters.Inbound.Api.Notifiers;
using FresiaFlow.Application.Ports.Outbound;
using Moq;
using Microsoft.AspNetCore.SignalR;

namespace FresiaFlow.Tests.Adapters;

public class SignalRSyncProgressNotifierTests
{
    [Fact]
    public async Task NotifyAsync_ShouldSendToAllClients()
    {
        var clientsMock = new Mock<IHubClients>();
        var allClientProxy = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.All).Returns(allClientProxy.Object);

        var hubContextMock = new Mock<IHubContext<SyncProgressHub>>();
        hubContextMock.SetupGet(h => h.Clients).Returns(clientsMock.Object);

        var notifier = new SignalRSyncProgressNotifier(hubContextMock.Object);
        var update = new SyncProgressUpdate { CurrentFile = "test", Percentage = 10 };

        await notifier.NotifyAsync(update);

        allClientProxy.Verify(c => c.SendCoreAsync("ReceiveProgress", It.Is<object[]>(args => args[0] == update), default), Times.Once);
    }
}

