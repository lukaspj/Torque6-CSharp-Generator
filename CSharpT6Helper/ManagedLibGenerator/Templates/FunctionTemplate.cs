using System;
using System.Linq;

namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   internal class FunctionTemplate
   {
      public static string GetMethodString(string internalName, T6Function t6Function)
      {
         string paramString = t6Function.Parameters.Aggregate("",
                  (x, y) => x + ", " + y.ParamType.NativeArgType + " " + y.ParamName);
         if (paramString.Length > 0)
            paramString = paramString.Substring(2);
         string argString = t6Function.Parameters.Aggregate("",
            (x, y) => x + ", " + y.ParamName);
         if (argString.Length > 0)
            argString = argString.Substring(2);


         string returnString =
            t6Function.ReturnType.GetReturn($"InternalUnsafeMethods.{internalName}({argString})");
         if (t6Function.ReturnType.ManagedType != "void")
            returnString = "return " + returnString;

         return
            $@"

      public static {t6Function.ReturnType.ManagedType} {t6Function.Name}({paramString})
      {{
         {returnString};
      }}";
      }
   }
}