using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericSignalR
{
    public class SignalRMethodAttribute : Attribute
    {
        public SignalRMethodAttribute()
        { }

        public SignalRMethodAttribute(string methodName)
        {
            MethodName = methodName;
        }

        public string? MethodName { get; set; }
    }
}
