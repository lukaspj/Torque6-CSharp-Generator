using System.Collections.Generic;
using System.Text.RegularExpressions;
using CSharpT6Helper;

internal class CFunction
{
   public CFunction(string pValue, string pName, CaptureCollection pCaptures)
   {
      FunctionVariableTypeInfo = T6Field.TypeFromString(pValue);
      FunctionName = pName;
      FunctionParams = new List<Param>();
      foreach (Capture capture in pCaptures)
      {
         int lastSpace = capture.Value.Trim().LastIndexOf(' ');
         string type = capture.Value.Trim().Substring(0, lastSpace).Trim();
         string name = capture.Value.Trim().Substring(lastSpace).Trim();
         FunctionParams.Add(new Param
         {
            Name = name,
            ParamTypeInfo = T6Field.TypeFromString(type)
         });
      }
   }

   public T6Field.TypeData FunctionVariableTypeInfo { get; set; }
   public List<Param> FunctionParams { get; set; }
   public string FunctionName { get; set; }
   public string FunctionBody { get; set; }
   public string FunctionDocumentation { get; set; }
   public EngineClass OwnerClass { get; set; }
   public string NameSpace { get; set; }
   public bool PropertyFunction { get; set; }

   internal class Param
   {
      public T6Field.TypeData ParamTypeInfo { get; set; }
      public string Name { get; set; }
      public string DefaultValue { get; set; }
   }
}