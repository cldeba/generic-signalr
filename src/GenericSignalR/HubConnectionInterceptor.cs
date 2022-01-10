using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Reflection;

namespace GenericSignalR
{
    internal class HubConnectionInterceptor : IInterceptor
    {
        private HubConnection hubConnection;

        public HubConnectionInterceptor(HubConnection hubConnection)
        {
            this.hubConnection = hubConnection;
        }

		public void Intercept(IInvocation invocation)
		{
			Type returnType = invocation.Method.ReturnType;

			bool isTask = typeof(Task).IsAssignableFrom(returnType);

			if (isTask && returnType.IsGenericType)
			{
				Type tcsType = typeof(TaskCompletionSource<>)
						  .MakeGenericType(returnType.GetGenericArguments()[0]);
				var tcs = Activator.CreateInstance(tcsType);
				invocation.ReturnValue = tcsType.GetProperty("Task").GetValue(tcs, null);

				InterceptAsync(invocation).ContinueWith(task =>
				{
					if (task.Exception == null)
						tcsType.GetMethod("SetResult").Invoke(tcs, new object[] { invocation.ReturnValue });
					else
						tcsType.GetMethod("SetException").Invoke(tcs, new object[] { task.Exception });
				});
			}
			else if (isTask)
			{
				Type tcsType = typeof(TaskCompletionSource);
				var tcs = Activator.CreateInstance(tcsType);
				invocation.ReturnValue = tcsType.GetProperty("Task").GetValue(tcs, null);

				hubConnection.InvokeCoreAsync(invocation.Method.Name, invocation.Method.ReturnType, invocation.Arguments).ContinueWith(task =>
				{
					if (task.Exception == null)
						tcsType.GetMethod("SetResult").Invoke(tcs, null);
					else
						tcsType.GetMethod("SetException").Invoke(tcs, new object[] { task.Exception });
				});
			}
            else
            {
				invocation.ReturnValue = hubConnection.InvokeCoreAsync(invocation.Method.Name, invocation.Method.ReturnType, invocation.Arguments).Result;
			}
		}

		private async Task InterceptAsync(IInvocation invocation)
		{
			object? response = await hubConnection.InvokeCoreAsync(invocation.Method.Name, invocation.Method.ReturnType.GenericTypeArguments[0], invocation.Arguments);

			invocation.ReturnValue = response;
		}
	}
}