using System;
using System.Linq;

namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   internal class FunctionTemplate
   {
      public static string GetMethodString(string internalName, T6Function t6Function)
      {
         string paramString = t6Function.GetParamString();
         string argString = t6Function.GetArgString();


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