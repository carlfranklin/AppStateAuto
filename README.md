# Managing Application State in .NET 8 Blazor Web Apps

### What is Application State?

Application State (or app state) represents collectively all of the variables (objects, lists, etc.) that you keep alive in your application while in use.  The default Blazor Sample App templates keep all variables in code blocks inside of pages. The problem with this approach is that those variables get reinitialized every time the page is navigated to or refreshed. 

The key to app state is to move it to a component that lives *outside* of the pages so that 

1. variables persist between navigations and page refreshes
2. variables can be shared by all pages and components

#### Goals of an AppState Component

1. We want UI to automatically update via binding whenever any of the properties update.
2. We may want to get control when the state gets mutated.

### Create a new Blazor Web App called AppStateAuto

![image-20231205131806324](images/image-20231205131806324.png)

![image-20231205140406871](images/image-20231205140406871.png)

Make sure you specify the following options:

![image-20231205140522355](images/image-20231205140522355.png)

> Interactive render mode: **Auto (Server and WebAssembly)**
>
> Interactivity Location: **Global**
>
> Include sample pages: **yes**

## Cascading Component

The approach we will take is to use a Cascading component. This is essentially an object reference that can be accessed by any component in the render tree below where it is defined. If we want all pages and components to have access to it, we can wrap it around the Router in *Routes.razor*, but I'm getting ahead of myself.

**Observe the default behavior**

Run the app (F5)

Go to the `Counter` page and increment the counter, now navigate to the `Home` page (*Home*) and back to `Counter`. Notice that the counter has been reset to zero!

This is because the counter value itself (currentCount) is defined within the page.

```c#
@page "/counter"

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

Whenever you navigate to a page, it is reinitialized. All of the module-level variables (such as currentCount) get reset to default values.

Let's fix that on our way to provide state to the entire app.

#### Add a new Razor Component to the Client project called CascadingAppState.razor

```c#
<CascadingValue Value="this">
    @ChildContent
</CascadingValue>

@code {

    [Parameter]
    public RenderFragment ChildContent { get; set; }

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
        }
    }

    protected override void OnInitialized()
    {
        Message = "Initial Message";
    }
}
```

#### What is a RenderFragment?

A `RenderFragment` represents a chunk of Razor markup that can then be rendered by the component.  Razor components can capture their child content as a `RenderFragment` and render that content as part of the component rendering. To capture child content, define a component parameter of type `RenderFragment` and name it `ChildContent`.

Using a RenderFragment named ChildContent tells the Blazor component engine that ChildContent is everything in the render tree below this component.

The property handlers must be explicit so that we can call `StateHasChanged()` when values are set. That tells the rendering engine that something has changed, and a redraw is necessary.

For more information on RenderFragments see https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-8.0#child-content-render-fragments:

#### Wrap the entire contents of Routes.razor in an instance of CascadingAppState

```xml
<CascadingAppState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)" />
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
    </Router>
</CascadingAppState>
```

#### Add a Razor Component to the Client called Toolbar.razor

```c#
﻿<div style="height:42px;">
    <span style="font-size:x-large">@AppState.Message</span>
</div>

@code {

    [CascadingParameter]
    public CascadingAppState AppState { get; set; }
}
```

The Toolbar will go across the top of the page. 

Note that we grab the reference to `CascadingAppState` with the `[CascadingParameter]` attribute. It's almost the same as injection, except that it's optimized for use in Blazor components and pages.

Now we can refer to `AppState` and it's properties anywhere in the component. The values of those properties exist OUTSIDE the page, and will still be there if we reload the component.

#### Modify Layout\MainLayout.razor to show the Toolbar

```xml
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <Toolbar/>
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>
```

#### Modify Pages\Home.razor

```c#
@page "/"

<PageTitle>Home</PageTitle>

<p>
    This is a demonstration of how to use a Cascading Parameter to share state between components
    in a .NET Blazor Web App using InteractiveAuto mode.
</p>

<button disabled="@uiDisabled" class="btn btn-primary" @onclick="UpdateMessageButtonClicked">Update Message</button>
<br />
<br />
<h3>@AppState.Message</h3>

@code
{
    [CascadingParameter]
    public CascadingAppState AppState { get; set; }

    bool uiDisabled = true;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            uiDisabled = false;
            StateHasChanged();
        }
    }

    void UpdateMessageButtonClicked()
    {
        AppState.Message = $"Message Updated At {DateTime.Now.ToLongTimeString()}";
    }
}
```

Again, we're grabbing that cascading reference to `CascadingAppState`. Now we have two components with access to it, and they can both get and set it's properties. 

#### Modify Pages\Counter.razor

```c#
@page "/counter"

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @AppState.Count</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {

    [CascadingParameter]
    public CascadingAppState AppState { get; set; }

    private void IncrementCount()
    {
        // This is the only place AppState.Count is incremented
        AppState.Count++;
    }
}
```

Here we have replaced currentCount with `AppState.Count`. 

Go to the `Counter` page, and click the button. Now navigate to the `Home` page and back to the `Counter` page.

Notice that our `counter` page remembers the value between navigations. That's because it is being stored in the `CascadingAppStateProvider`!

Click the **Update Message** button, and notice that the text in the Toolbar changes automatically.

## Getting control when an AppState variable is changed

Modify the *CascadingAppState.razor* file with this:

```c#
<CascadingValue Value="this">
    @ChildContent
</CascadingValue>

@code {

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
            PropertyChanged.InvokeAsync(new ("Message", value));
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
        }
    }

    protected override void OnInitialized()
    {
        Message = "Initial Message";
    }
}
```

Add *StatePropertyChangedArgs.cs* to the client:

```c@
/// <summary>
/// This record is used to pass the name of the property that 
/// changed and the new value of the property.
/// </summary>
/// <param name="PropertyName"></param>
/// <param name="NewValue"></param>
public record StatePropertyChangedArgs(string PropertyName, object? NewValue);
```

Now `CascadingAppState` exposes a `PropertyChanged` EventCallback.

### Why not just implement `INotifyPropertyChanged`?

You absolutely could. However, the Blazor Component Model gives us the `EventCallback`, the handlers for which do not have to be unhooked, and therefore the consumers of them do not need to implement `IDisposable`.

Modify *Toolbar.razor* to handle the `EventCallback`:

```c#
﻿<div style="height:42px;">
    <span style="font-size:x-large">@AppState.Message</span>
</div>

@code {

    [CascadingParameter]
    public CascadingAppState AppState { get; set; }

    protected override void OnInitialized()
    {
        // Subscribe to the PropertyChanged event.

        // We do not need to implement IDisposable because it's an EventCallback, not an event.

        AppState.PropertyChanged = 
            EventCallback.Factory.Create<StatePropertyChangedArgs>(this, HandlePropertyChanged);
    }

    private void HandlePropertyChanged(StatePropertyChangedArgs args)
    {
        // Now the toolbar can be notified when any AppState property changes, and retrieve the new value
    }
}
```

Set a breakpoint in the `Toolbar`'s `HandlePropertyChanged` method. Run the app and click the **Update Message** button.

![image-20231205141213898](images/image-20231205141213898.png)

![image-20231205141254799](images/image-20231205141254799.png)

If you inspect the `args` value, the `PropertyName` will be "Message", and `NewValue` will be the value that it was just set to.

#### Why?

Why would want to know when another component updates an AppState property? 

If your goal is just to reflect the state variables in the UI when they change, you do not need to react to `PropertyChanged`.

However, there may be a situation when you need to run some code, save data to a database, or do any other code action when a value changes. Now you have a way to do that.

## Persisting Application State

It would be great if we could save certain `AppState` properties so that they will come back the next time the app is run. 

We will need access to LocalStorage. For this we will use Chris Sainty's `Blazored.LocalStorage` pagckage.

Add the following package reference to both the server and client projects:

```xml
<PackageReference Include="Blazored.LocalStorage" Version="4.4.0" />
```

Add this to *Program.cs* in both projects:

```c#
builder.Services.AddBlazoredLocalStorage();
```

You'll need this:

```c#
using Blazored.LocalStorage;
```

We are going to separate the properties we want to persist into an interface.

That will allow us to serialize just those properties to JSON.

Add the following class to the client project:

*IAppState.cs*

```c#
namespace AppStateAuto.Client;

public interface IAppState
{
    string Message { get; set; }
    int Count { get; set; }
    DateTime LastStorageSaveTime { get; set; }
}
```

`System.Text.Json` will not deserialize to an interface, so we have to create a class that implements the interface:

```c#
namespace AppStateAuto.Client;

public class AppState : IAppState
{
    public string Message { get; set; } =  string.Empty;
    public int Count { get; set; }
    public DateTime LastStorageSaveTime { get; set; }
}
```

Now replace *CascadingAppState.razor* with the following:

```c#
<CascadingValue Value="this">
    @ChildContent
</CascadingValue>
```

Add a code-behind file named *CascadingAppState.razor.cs*:

```c#
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
```

Notice that we are implementing `IAppState`  so we can easily serialize and deserialize just those properties.

Check out `Save()`:

```c#
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
```

This gets called from our property setters. 

Any time a property is changed, all the values of all the properties in `IAppState` will be saved in local storage.

> ***NOTE***: Everything still works as before, but now we can persist all the properties exposed by the Interface.

Now check out  `Load()`, which is called from`OnAfterRenderAsync` :

```c#
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
```

This happens after the first render.

We're reading the JSON from local storage, deserializing it into an `AppState` object (remember, we can't serialize to an interface), and then using reflection to set the values that are part of our interface.

Press F5 and press the **Update Message** button. You can also update the counter if you like.

![image-20231205141336641](images/image-20231205141336641.png)

Close the app right after making a change, and then run it again within 30 seconds. The AppState will load from LocalStorage:

![image-20231205141336641](images/image-20231205141336641.png)

If you close the app and wait at least 30 seconds before running it again, the AppState gets reset and you're back to square one.

![image-20231205141420676](images/image-20231205141420676.png)

### Only Persist Certain Properties

The `IAppStae` interface and the `AppState` class determine which properties get persisted, and you can modify them at will. The only requirement is that the properties must be implemented in *CascadingAppState.razor*.

Try it! Remove a property from both `IAppState` and `AppState`. Run the app and test the persistence.

