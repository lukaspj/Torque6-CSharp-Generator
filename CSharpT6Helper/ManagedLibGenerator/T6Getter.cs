using System.Collections.Generic;

namespace CSharpT6Helper
{
   internal class T6Getter : T6Function
   {
      public T6Getter(string pPropGetterName, CSharpGenerator.CSharpType pPropReturnType, string pGetterName,
         CSharpGenerator.CSharpType pGetterType)
         : base(pGetterName, null, pGetterType)
      {
         PropGetterName = pPropGetterName;
         PropReturnType = pPropReturnType;
         GetterName = pGetterName;
         GetterType = pGetterType;
         HasIndex = false;
      }

      public CSharpGenerator.CSharpType GetterType { get; set; }
      public string GetterName { get; set; }
      public CSharpGenerator.CSharpType PropReturnType { get; set; }
      public string PropGetterName { get; set; }
      public bool HasIndex { get; set; }

      public override string GetParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return Parameters[0].ParamType.NativeArgType + " " + Parameters[0].ParamName + indexString;
      }

      public override string GetNativeParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return Parameters[0].ParamType.NativeArgType + " " + Parameters[0].ParamName + indexString;
      }

      public override string GetArgString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", index";
         return Parameters[0].ParamName + indexString;
      }

      public override string GetNativeArgString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", index";
         return Parameters[0].ParamName + indexString;
      }
   }
}