using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(string eventType, string content, CancellationToken cancellationToken);
}

public sealed class NullIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync(string eventType, string content, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Identity outbox dispatch failed.");
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var messages = await dbContext.OutboxMessages.Where(x => x.ProcessedOnUtc == null)
            .OrderBy(x => x.OccurredOnUtc).Take(20).ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Content, cancellationToken);
                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception exception)
            {
                message.Error = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            }
        }

        if (messages.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
    }
}
