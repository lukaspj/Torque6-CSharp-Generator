namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   class NamespaceTemplate
   {
      public static string GetClassString(string name,
         string unsafeNativeMethodCodeBlock,
         string methodsCodeBlock,
         string additionalCodeBlock)
      {
         return
$@"using System;
using System.Runtime.InteropServices;
using Torque6.Engine.SimObjects;
using Torque6.Engine.SimObjects.Scene;
using Torque6.Utility;

namespace Torque6.Engine.Namespaces
{{
   public static unsafe class {name}
   {{
      #region UnsafeNativeMethods

      new internal struct InternalUnsafeMethods
      {{
         {unsafeNativeMethodCodeBlock}
      }}

      #endregion
      
      #region Functions

      {methodsCodeBlock}      

      #endregion

      {additionalCodeBlock}
   }}
}}";
      }
   }
}
