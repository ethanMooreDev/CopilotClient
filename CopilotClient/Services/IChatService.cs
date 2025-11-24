using CopilotClient.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotClient.Services;

public interface IChatService
{
    Task<ChatMessage> SendAsync(IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default);
}