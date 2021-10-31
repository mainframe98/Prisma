using System;
using System.Collections.Generic;

namespace Prisma.Config
{
    public class CgiApplicationConfig : ICloneable
    {
         /// <summary>
         /// Arguments to invoke the application with.
         /// </summary>
         public List<string> Arguments { get; set; } = new();

         /// <summary>
         /// Environment variables to set when invoking the application.
         /// </summary>
         public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

         /// <summary>
         /// Path to the CGI application.
         /// </summary>
         public string Path { get; set; } = "";

         public object Clone()
         {
             CgiApplicationConfig clone = (CgiApplicationConfig)this.MemberwiseClone();

             // This works because the types in the list and dictionary are value types.
             clone.Arguments = new List<string>(this.Arguments);
             clone.EnvironmentVariables = new Dictionary<string, string>(this.EnvironmentVariables);

             return clone;
         }
    }
}
