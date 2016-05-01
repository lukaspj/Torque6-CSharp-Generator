using System.ComponentModel;

namespace CSharpT6Helper
{
   internal class T6OutParamGetter : T6Getter
   {
      public T6OutParamGetter(string pPropGetterName, CSharpGenerator.CSharpType pPropReturnType, string pGetterName,
         CSharpGenerator.CSharpType pGetterType)
         : base(pPropGetterName, pPropReturnType, pGetterName, pGetterType)
      {
      }

      public T6OutParamGetter(string pPropGetterName, CSharpGenerator.CSharpType pPropReturnType, string pGetterName,
         CSharpGenerator.CSharpType pGetterType,
         string pOutParamName, CSharpGenerator.CSharpType pOutParamType)
         : base(pPropGetterName, pPropReturnType, pGetterName, pGetterType)
      {
         OutParamName = pOutParamName;
         OutParamType = pOutParamType;
      }

      public CSharpGenerator.CSharpType OutParamType { get; set; }
      public string OutParamName { get; set; }

      public override string GetParamString()
      {
         return base.GetParamString() + ", out " + OutParamType.NativeArgType + " " + OutParamName;
      }

      public override T6Function ToT6Function()
      {
         T6Function function = base.ToT6Function();
         function.Parameters[2].ParamName = function.Parameters[2].ParamName.Insert(2, " ");
         return function;
      }
   }
}