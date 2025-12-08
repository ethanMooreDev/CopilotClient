using CopilotClient.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotClient.Services;

public interface IChatService
{
    Task<ChatMessage> SendAsync(ServiceConversation request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(ServiceConversation request, CancellationToken cancellationToken = default);

    Task<string> SummarizeAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default);
}
