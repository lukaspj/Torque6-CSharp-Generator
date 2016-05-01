namespace CSharpT6Helper
{
   internal class T6Parameter
   {
      public T6Parameter(string pParamName, CSharpGenerator.CSharpType pParamType, string pParamDefault)
      {
         ParamName = pParamName;
         ParamType = pParamType;
         ParamDefault = pParamDefault;
      }

      public CSharpGenerator.CSharpType ParamType { get; set; }
      public string ParamName { get; set; }
      public string ParamDefault { get; set; }
   }
}