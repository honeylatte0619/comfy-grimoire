using ComfyGrimoire.Helpers;

namespace ComfyGrimoire.Services;

public class HistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FileName { get; set; } = string.Empty;
    public GenerationInfo Info { get; set; } = new();
}

public class HistoryService
{
    public List<HistoryItem> Items { get; private set; } = new();
    
    public event Action? OnChange;
    public event Action<HistoryItem>? OnItemSelected;

    public void AddItem(GenerationInfo info, string fileName)
    {
        var item = new HistoryItem
        {
            FileName = fileName,
            Info = info
        };
        
        Items.Insert(0, item);
        OnChange?.Invoke();
    }

    public void SelectItem(HistoryItem item)
    {
        OnItemSelected?.Invoke(item);
    }
}
