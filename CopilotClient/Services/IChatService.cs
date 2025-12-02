using System.Threading;
using System.Threading.Tasks;
using CopilotClient.Models;

namespace CopilotClient.Services;

public interface IChatService
{
    Task<ChatMessage> SendAsync(ServiceConversation request, CancellationToken cancellationToken = default);
}
