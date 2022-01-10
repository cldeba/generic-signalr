# Generic SignalR
Provides client-side functionality to execute generic client-to-server and server-to-client invocations using SignalR. Use this package to improve type-safety and code quality when making use of SignalR features. This package makes the unsafe use of  HubConnection's On and SendMessage methods obsolete.

![NuGet](https://img.shields.io/nuget/v/GenericSignalR)

## Getting started

To make use of this library's functionality use the package ```Microsoft.AspNetCore.SignalR.Client``` to create a ```HubConnection``` instance. Refere to [this link](https://docs.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-6.0&tabs=visual-studio) for further information.

```csharp
using Microsoft.AspNetCore.SignalR.Client;

//...

HubConnection connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:53353/ChatHub")
    .Build();
```


## Handler for server-to-client invocations

First, create a Handler class that will  be used for handling server-to-client invocations.

```csharp
using GenericSignalR;

class Handler
{
    // This method gets called when the server invokes "SomeMethod" on the client.
    public async Task SomeMethod()
    {
        Console.WriteLine("SomeMethod called");
    }
    
    // This method gets called when the server invokes "RenamedMethod" on the client.
    [SignalRMethod("RenamedMethod")]
    public async Task SomeOtherMethod()
    {
        Console.WriteLine("RenamedMethod called");
    }

    // This method is ignored and will not handle server-to-client invocations.
    [SignalRIgnore]
    public void IgnoredMethod()
    {

    }
}
```

Then use the ```UseHandler<THandler>``` extension method to register a instance of the previously created Handler class:

```csharp
connection.UseHandler(new Handler());
```

This method automatically removes all existing handlers to any of the handler's method names. If you want to keep existing handlers, set the optional ```bool removeExistingMethodHandlers``` parameter to false.

## Using a proxy interface for client-to-server invocations

To perform client-to-server invocations, create an interface that declares the relevant methods (these methods must be declared in the SignalR server hub too).

```csharp
public interface IHub
{
    Task<string> TestMethod();
    Task TestMethodNoReturnType();
    string TestMethodNonTask();
}
```

Then call the ```GetRemoteHubProxy<THub>``` extension method to create a proxy implementation of the hub interface. This method uses the ```Castle.Core``` package to dynamically create a proxy implementation. Whenever a method of the proxy instance is called, the invocation is automatically forwared to the HubConnection instance. This method supports return values, Tasks with return values and Tasks without return values.

```csharp
IHub hub = connection.GetRemoteHubProxy<IHub>();
```

That's it! You'll never have to unsafely use the ```HubConnection``` class' ```On``` and ```SendMessage``` methods.

## Architecture considerations

When designing an ASP.NET Core application with SignalR, you can introduce a shared Hub interface (in a shared project referenced both by server and client projects) that is
 * implemented by the server-side Hub and is also
 * used for client-to-server invocations on the client side

That way you don't have to worry about breaking changes in the Hub implementation as you will get a compile-time error when building the client if a method was renamed or removed from the shared Hub interface or any of the parameters changed.
