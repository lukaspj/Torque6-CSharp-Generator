using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   internal class NativeFunctionTemplate
   {
      public static string GetFunctionString(T6Function t6Function)
      {
         return GetFunctionString(t6Function, t6Function.Name);
      }

      public static string GetFunctionString(string nsName, T6Function t6Function)
      {
         return GetFunctionString(t6Function, nsName + "_" + t6Function.Name);
      }

      public static string GetFunctionString(T6Function t6Function, string internalName)
      {
         return GetFunctionString(t6Function.Parameters,
            t6Function.ReturnType.NativeReturnType,
            internalName);
      }

      public static string GetFunctionString(List<T6Parameter> parameters, 
         string nativeReturnType, 
         string internalName)
      {
         string paramString = parameters.Aggregate("",
                  (x, y) => x + ", " + y.ParamType.NativeArgType + " " + y.ParamName);
         if (paramString.Length > 0)
            paramString = paramString.Substring(2);
         string argString = parameters.Aggregate("",
            (x, y) => x + ", " + y.ParamName);
         if (argString.Length > 0)
            argString = argString.Substring(2);
         
         string retString = "_" + internalName + "Func";
         if (nativeReturnType != "void")
            retString = "return " + retString;

         return String.Format(@"

         [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
         private delegate {1} _{0}({2});
         private static _{0} _{0}Func;
         internal static {1} {0}({2})
         {{
            if (_{0}Func == null)
            {{
               _{0}Func =
                  (_{0})Marshal.GetDelegateForFunctionPointer(Interop.Torque6.DllLoadUtils.GetProcAddress(Interop.Torque6.Torque6LibHandle,
                     ""{0}""), typeof(_{0}));
            }}

            {4}({3});
         }}", internalName, nativeReturnType, paramString, argString, retString);
      }
   }
}