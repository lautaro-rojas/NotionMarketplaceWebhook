using System;
using System.Threading;
using System.Threading.Tasks;

namespace NotionWebhookService.Services
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
        ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
