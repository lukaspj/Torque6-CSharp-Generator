using System.Collections.Generic;
using System.Linq;

namespace CSharpT6Helper
{
   internal class T6Function
   {
      public T6Function(string pName, string pNameSpace, CSharpGenerator.CSharpType pReturnType)
      {
         Parameters = new List<T6Parameter>();
         Name = pName;
         NameSpace = pNameSpace;
         ReturnType = pReturnType;
      }

      public CSharpGenerator.CSharpType ReturnType { get; set; }
      public string Name { get; set; }
      public string NameSpace { get; set; }
      public List<T6Parameter> Parameters { get; set; }

      public virtual string GetNativeParamString()
      {
         string paramString = Parameters.Aggregate("",
            (x, y) => x + ", " + y.ParamType.NativeArgType + " " + y.ParamName);
         if (paramString.Length > 0)
            paramString = paramString.Substring(2);
         return paramString;
      }

      public virtual string GetParamString()
      {
         string paramString = Parameters.Aggregate("",
            (x, y) =>
            {
               x += ", " + y.ParamType.ManagedType + " " + y.ParamName;
               if (y.ParamDefault != null) x += " = " + y.ParamDefault;
               return x;
            });
         if (paramString.Length > 0)
            paramString = paramString.Substring(2);
         return paramString;
      }

      public virtual string GetNativeArgString()
      {
         string argString = Parameters.Aggregate("",
            (x, y) => x + ", " + y.ParamName);
         if (argString.Length > 0)
            argString = argString.Substring(2);
         return argString;
      }

      public virtual string GetArgString()
      {
         string argString = Parameters.Aggregate("",
            (x, y) => x + ", " + y.ParamType.GetArg(y.ParamName));
         if (argString.Length > 0)
            argString = argString.Substring(2);
         return argString;
      }
   }
}