using System.Text.Json;
using System.Text.Json.Nodes;

namespace ComfyGrimoire.Helpers;

public static class ComfyMetadataParser
{
    public static GenerationInfo Parse(string promptJson)
    {
        var info = new GenerationInfo();
        if (string.IsNullOrWhiteSpace(promptJson)) return info;

        try
        {
            var jsonNode = JsonNode.Parse(promptJson);
            if (jsonNode == null) return info;

            // Iterate through all nodes to find KSampler and CheckpointLoader
            foreach (var child in jsonNode.AsObject())
            {
                var node = child.Value;
                if (node == null) continue;

                var classType = node["class_type"]?.ToString();
                var inputs = node["inputs"];

                if (string.IsNullOrEmpty(classType) || inputs == null) continue;

                if (classType == "KSampler" || classType == "KSamplerAdvanced")
                {
                    if (inputs["seed"] != null) info.Seed = (long)inputs["seed"]!;
                    if (inputs["steps"] != null) info.Steps = (int)inputs["steps"]!;
                    if (inputs["cfg"] != null) info.Cfg = (float)inputs["cfg"]!;
                    if (inputs["sampler_name"] != null) info.SamplerName = inputs["sampler_name"]?.ToString() ?? string.Empty;
                    if (inputs["scheduler"] != null) info.Scheduler = inputs["scheduler"]?.ToString() ?? string.Empty;

                    // Try to trace back prompts
                    if (inputs["positive"] is JsonArray posRef && posRef.Count > 0)
                    {
                        info.PositivePrompt = ExtractText(jsonNode, posRef[0]?.ToString());
                    }
                    if (inputs["negative"] is JsonArray negRef && negRef.Count > 0)
                    {
                        info.NegativePrompt = ExtractText(jsonNode, negRef[0]?.ToString());
                    }
                }
                else if (classType == "CheckpointLoaderSimple" || classType == "CheckpointLoader")
                {
                    if (inputs["ckpt_name"] != null) info.Checkpoint = inputs["ckpt_name"]?.ToString() ?? string.Empty;
                }
                else if (classType == "LoraLoader")
                {
                    if (inputs["lora_name"] != null)
                    {
                        var loraName = inputs["lora_name"]?.ToString();
                        var strength = inputs["strength_model"]?.ToString() ?? "1.0";
                         if (!string.IsNullOrEmpty(loraName))
                        {
                            info.Loras.Add($"{loraName} (Str: {strength})");
                        }
                    }
                }
            }

            // Extract LoRAs from prompt text using Regex
            ExtractLorasFromText(info.PositivePrompt, info.Loras);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing ComfyUI metadata: {ex.Message}");
        }

        return info;
    }

    private static void ExtractLorasFromText(string text, List<string> loras)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // Pattern: <lora:LoraName:1.0> or <lora:LoraName>
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"<lora:([^:>]+)(?::([0-9.]+))?>");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var name = match.Groups[1].Value;
            var strength = match.Groups[2].Success ? match.Groups[2].Value : "1.0";
            var output = $"{name} (Str: {strength})";
            
            // Avoid duplicates
            if (!loras.Contains(output))
            {
                loras.Add(output);
            }
        }
    }

    private static string ExtractText(JsonNode root, string? nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return string.Empty;
        
        var node = root[nodeId];
        if (node == null) return string.Empty;

        var inputs = node["inputs"];
        if (inputs == null) return string.Empty;

        // Search for common keys used for prompts/text
        string[] potentialKeys = { "text", "text_g", "text_l", "prompt", "string", "caption" };

        foreach (var key in potentialKeys)
        {
            if (inputs[key] != null)
            {
                var text = inputs[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        
        return string.Empty; 
    }
}
