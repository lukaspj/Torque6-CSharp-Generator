namespace CSharpT6Helper
{
   internal class T6OutParamGetter : T6Getter
   {
      public T6OutParamGetter(string pPropGetterName, CSharpGenerator.CSharpType pPropReturnType, string pGetterName,
         CSharpGenerator.CSharpType pGetterType)
         : base(pPropGetterName, pPropReturnType, pGetterName, pGetterType)
      {
         OutParamName = pPropGetterName;
         OutParamType = pPropReturnType;
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

      public T6Parameter OutParam => Parameters[HasIndex ? 2 : 1];

      public override string GetParamString()
      {
         return base.GetParamString() + ", out " + OutParam.ParamType.NativeArgType + " " + OutParam.ParamName;
      }

      public override string GetNativeParamString()
      {
         return base.GetNativeParamString() + ", out " + OutParam.ParamType.NativeArgType + " " + OutParam.ParamName;
      }

      public override string GetNativeArgString()
      {
         return base.GetNativeArgString() + ", out " + OutParam.ParamName;
      }
   }
}