using System.Collections.Generic;

namespace CSharpT6Helper
{
   internal class T6Setter : T6Function
   {
      public T6Setter(string pPropSetterName, CSharpGenerator.CSharpType pPropReturnType, string pSetterName,
         CSharpGenerator.CSharpType pSetterType)
         : base(pSetterName, null, pSetterType)
      {
         PropSetterName = pPropSetterName;
         PropReturnType = pPropReturnType;
         SetterName = pSetterName;
         SetterType = pSetterType;
         HasIndex = false;
      }

      public string PropSetterName { get; set; }
      public CSharpGenerator.CSharpType PropReturnType { get; set; }
      public string SetterName { get; set; }
      public CSharpGenerator.CSharpType SetterType { get; set; }
      public bool HasIndex { get; set; }

      public T6Parameter ValueParameter => Parameters[HasIndex ? 2 : 1];

      public override string GetParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return Parameters[0].ParamType.NativeArgType + " " + Parameters[0].ParamName + indexString
                + ", " + ValueParameter.ParamType.NativeArgType + " " + ValueParameter.ParamName;
      }

      public override string GetArgString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", index";
         return Parameters[0].ParamName + indexString
                + ", " + ValueParameter.ParamName;
      }
   }
}