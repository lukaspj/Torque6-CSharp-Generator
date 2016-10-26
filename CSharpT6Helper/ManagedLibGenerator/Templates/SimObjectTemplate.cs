using System.Collections.Generic;
using System.Linq;

namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   class SimObjectTemplate
   {
      public static string GetClassString(string ns, 
         string name, 
         List<string> parents,
         string constructorsCodeBlock,
         string unsafeNativeMethodCodeBlock, 
         string propertiesCodeBlock,
         string methodsCodeBlock,
         string additionalCodeBlock)
      {
         string parentString = "";
         if (parents != null)
         {
            parentString = " : " + parents.Aggregate("", (x, y) => x + ", " + y).Substring(2);
         }

         return
$@"using System;
using System.Linq;
using System.Runtime.InteropServices;
using Torque6.Engine.SimObjects;
using Torque6.Engine.SimObjects.Scene;
using Torque6.Engine.Namespaces;
using Torque6.Interop;
using Torque6.Utility;

namespace {ns}
{{
   public unsafe class {name}{parentString}
   {{
      {constructorsCodeBlock}
      
      #region UnsafeNativeMethods

      new internal struct InternalUnsafeMethods
      {{
         {unsafeNativeMethodCodeBlock}
      }}
      
      #endregion

      #region Properties

      {propertiesCodeBlock}
      
      #endregion
      
      #region Methods

      {methodsCodeBlock}
      
      #endregion

      {additionalCodeBlock}
   }}
}}";
      }
   }
}
