using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CopilotClient.Models;

namespace CopilotClient.Persistence;

public class JsonConversationStore : IConversationStore
{
    private readonly string _filePath;

    public JsonConversationStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopilotClient");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "conversations.json");
    }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private async Task<List<Conversation>> LoadAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<Conversation>();

        await using var stream = File.OpenRead(_filePath);
        var conversations = await JsonSerializer.DeserializeAsync<List<Conversation>>(stream, _options)
                           ?? new List<Conversation>();

        // normalize transient message states on load
        foreach (var convo in conversations)
        {
            foreach (var msg in convo.Messages)
            {
                if (msg.Status is MessageStatus.Sending or MessageStatus.Typing or MessageStatus.Streaming)
                {
                    msg.Status = MessageStatus.Failed;
                }
            }
        }

        return conversations;
    }

    private async Task SaveAllAsync(List<Conversation> conversations)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, conversations, _options);
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetSummariesAsync()
    {
        var all = await LoadAllAsync();

        return all
            .OrderByDescending(c => c.LastUpdatedAt)
            .Select(c => new ConversationSummary
            {
                Id = c.Id,
                Title = c.Title,
                Mode = c.Mode,
                LastUpdatedAt = c.LastUpdatedAt
            })
            .ToList();
    }

    public async Task<Conversation?> LoadConversationAsync(Guid id)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(c => c.Id == id);
    }

    public async Task SaveConversationAsync(Conversation conversation)
    {
        var all = await LoadAllAsync();

        var index = all.FindIndex(c => c.Id == conversation.Id);

        conversation.LastUpdatedAt = DateTime.UtcNow;

        if (index >= 0)
            all[index] = conversation;
        else
            all.Add(conversation);

        await SaveAllAsync(all);
    }

    public async Task DeleteConversationAsync(Guid id)
    {
        var all = await LoadAllAsync();
        all.RemoveAll(c => c.Id == id);
        await SaveAllAsync(all);
    }
}
