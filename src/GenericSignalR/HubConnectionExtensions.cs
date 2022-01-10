using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GenericSignalR
{
    public static class HubConnectionExtensions
    {
        /// <summary>
        /// Registers a handler instance for server-to-client invocations. When the server sends invocation calls the method is automatically mapped to the handler instance. Therefore, this method replaces the <see cref="HubConnection.On(string, Type[], Func{object?[], object, Task}, object)"/> calls.
        /// </summary>
        /// <remarks>
        /// This method only resepects methods that are declared by the Handler type (THandler) itself. Inherited methods are not supported.
        /// </remarks>
        /// <typeparam name="THandler">The type of the handler class.</typeparam>
        /// <param name="hubConnection">The <see cref="HubConnection"/> instance that should be used for server-to-client invocation mapping.</param>
        /// <param name="handler">The handler instance.</param>
        public static void UseHandler<THandler>(this HubConnection hubConnection, THandler handler, bool removeExistingMethodHandlers = true)
        {
            Type clientType = typeof(THandler);

            List<MethodInfo> methods = new List<MethodInfo>();

            foreach (MethodInfo method in clientType.GetMethods())
            {
                // Only use methods that are directly declared by the handler (no methods from inherited types)
                if (method.DeclaringType != typeof(THandler))
                    continue;

                // Ignore method if [SignalRIgnore] attribute is specified
                if (method.GetCustomAttribute<SignalRIgnoreAttribute>() != null)
                    continue;

                if (method.ReturnType != typeof(Task))
                    throw new InvalidOperationException($"Cannot register handler {typeof(THandler).FullName}. All client handler methods must return '{typeof(Task).FullName}'.");

                methods.Add(method);
            }

            foreach (MethodInfo method in methods)
            {
                SignalRMethodAttribute? signalRMethod = method.GetCustomAttribute<SignalRMethodAttribute>();

                string methodName = signalRMethod?.MethodName ?? method.Name;

                if (removeExistingMethodHandlers)
                    hubConnection.Remove(methodName);

                hubConnection.On(methodName, method.GetParameters().Select(p => p.ParameterType).ToArray(),
                    px =>
                    {
                        object? result = method.Invoke(handler, px);
                        if (result is Task task)
                            return task;
                        return Task.CompletedTask;
                    });
            }
        }

        /// <summary>
        /// Gets a proxy implementation of the specified hub interface which can be used to execute client-to-server invocations.
        /// </summary>
        /// <remarks>
        /// This method supports Task invocations.
        /// </remarks>
        /// <typeparam name="TRemoteHub">The hub interface a proxy implementation should be provided for. This type must be an interface type.</typeparam>
        /// <param name="hubConnection">The <see cref="HubConnection"/> instance that should be used for the proxy generation.</param>
        /// <returns>A proxy implementation of the specified hub interface type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the hub interface type (TRemoteHub) is not an interface type.</exception>
        public static TRemoteHub GetRemoteHubProxy<TRemoteHub>(this HubConnection hubConnection)
        {
            Type interfaceType = typeof(TRemoteHub);
            if (!interfaceType.IsInterface)
                throw new InvalidOperationException("TRemoteHub must be an interface type.");

            ProxyGenerator proxyGenerator = new ProxyGenerator();
            return (TRemoteHub) proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(TRemoteHub), new HubConnectionInterceptor(hubConnection));
        }
    }
}