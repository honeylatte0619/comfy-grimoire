namespace ComfyGrimoire.Helpers;

public class GenerationInfo
{
    public string Checkpoint { get; set; } = string.Empty;
    public long Seed { get; set; }
    public int Steps { get; set; }
    public float Cfg { get; set; }
    public string SamplerName { get; set; } = string.Empty;
    public string Scheduler { get; set; } = string.Empty;
    public string PositivePrompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public List<string> Loras { get; set; } = new();
    
    public string RawPromptJson { get; set; } = string.Empty;
    public string RawWorkflowJson { get; set; } = string.Empty;

    public bool HasData => !string.IsNullOrEmpty(PositivePrompt) || !string.IsNullOrEmpty(Checkpoint);
}
