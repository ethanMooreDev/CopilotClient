using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotClient.Models;

namespace CopilotClient.Persistence;

public interface IConversationStore
{
    Task<IReadOnlyList<ConversationSummary>> GetSummariesAsync();

    Task<Conversation?> LoadConversationAsync(Guid id);

    Task SaveConversationAsync(Conversation conversation);

    Task DeleteConversationAsync(Guid id);
}