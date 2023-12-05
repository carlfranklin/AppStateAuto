using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using System.Text.Json;

namespace AppStateAuto.Client;

public partial class CascadingAppState : ComponentBase, IAppState
{
    private readonly string StorageKey = "MyAppStateKey";

    private readonly int StorageTimeoutInSeconds = 30;

    bool loaded = false;

    public DateTime LastStorageSaveTime { get; set; }

    [Inject]
    ILocalStorageService localStorage { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// This EventCallback is raised when any property of the AppState changes
    /// </summary>
    [Parameter]
    public EventCallback<StatePropertyChangedArgs> PropertyChanged { get; set; }

    /// <summary>
    /// Implement property handlers like so
    /// </summary>
    private string message = "";
    public string Message
    {
        get => message;
        set
        {
            message = value;
            StateHasChanged();  // Optionally force a re-render
            PropertyChanged.InvokeAsync(new("Message", value));
            new Task(async () =>
            {
                await Save();
            }).Start();
        }
    }

    private int count = 0;
    public int Count
    {
        get => count;
        set
        {
            count = value;
            StateHasChanged();
            PropertyChanged.InvokeAsync(new("Count", value));
            new Task(async () =>
            {
                await Save();
            }).Start();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Load();
            loaded = true;
            StateHasChanged();
        }
    }

    protected override void OnInitialized()
    {
        Message = "Initial Message";
    }

    public async Task Save()
    {
        if (!loaded) return;

        // set LastSaveTime
        LastStorageSaveTime = DateTime.Now;
        // serialize 
        var state = (IAppState)this;
        // save
        await localStorage.SetItemAsync<IAppState>(StorageKey, state);
    }

    public async Task Load()
    {
        try
        {
            var json = await localStorage.GetItemAsStringAsync(StorageKey);
            if (json == null || json.Length == 0) return;
            var state = JsonSerializer.Deserialize<AppState>(json);
            if (state != null)
            {
                if (DateTime.Now.Subtract(state.LastStorageSaveTime).TotalSeconds <= StorageTimeoutInSeconds)
                {
                    // decide whether to set properties manually or with reflection

                    // comment to set properties manually
                    //this.Message = state.Message;
                    //this.Count = state.Count;

                    // set properties using Reflaction
                    var t = typeof(IAppState);
                    var props = t.GetProperties();
                    foreach (var prop in props)
                    {
                        if (prop.Name != "LastStorageSaveTime")
                        {
                            object value = prop.GetValue(state);
                            prop.SetValue(this, value, null);
                        }
                    }

                }
            }
        }
        catch (Exception ex)
        {

        }
    }
}
