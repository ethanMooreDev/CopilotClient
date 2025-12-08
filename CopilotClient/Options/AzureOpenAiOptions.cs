namespace CopilotClient.Options;

public sealed class AzureOpenAiOptions
{
    // e.g. "https://my-resource-name.openai.azure.com/"
    public string Endpoint { get; init; } = string.Empty;

    // Your deployment name in Azure OpenAI Studio
    public string DeploymentName { get; init; } = string.Empty;

    // API key for the Azure OpenAI resource
    public string ApiKey { get; init; } = string.Empty;

    public float Temperature { get; set; } = 0.3f;

    public int MaxOutputTokens { get; set; } = 512;

    public float ExplainTemperature { get; set; } = 0.3f;

    public int ExplainMaxOutputTokens { get; set; } = 512;
}
