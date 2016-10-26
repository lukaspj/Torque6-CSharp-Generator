using System;

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
         T6OutParamGetter getter = t6Function as T6OutParamGetter;
         if (getter != null)
         {
            return GetOutFunctionString(t6Function.GetNativeParamString(), t6Function.GetNativeArgString(),
               internalName,
               getter.OutParam.ParamType.NativeArgType);
         }
         return GetFunctionString(t6Function.GetNativeParamString(), t6Function.GetNativeArgString(),
            t6Function.ReturnType.NativeReturnType,
            internalName);
      }

      private static string GetOutFunctionString(string paramString, string argString,
         string internalName,
         string managedReturnType)
      {
         string retString = "_" + internalName + "Func";

         return String.Format(@"

         [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
         private delegate void _{0}({1});
         private static _{0} _{0}Func;
         internal static void {0}({1})
         {{
            if (_{0}Func == null)
            {{
               _{0}Func =
                  (_{0})Marshal.GetDelegateForFunctionPointer(Interop.Torque6.DllLoadUtils.GetProcAddress(Interop.Torque6.Torque6LibHandle,
                     ""{0}""), typeof(_{0}));
            }}
            {3}({2});
         }}", internalName, paramString, argString, retString, managedReturnType);
      }

      public static string GetFunctionString(string paramString, string argString,
         string nativeReturnType,
         string internalName)
      {

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