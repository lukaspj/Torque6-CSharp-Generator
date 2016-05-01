using System.Collections.Generic;

namespace CSharpT6Helper
{
   internal class T6Function
   {
      public T6Function(string pName, string pNameSpace, CSharpGenerator.CSharpType pReturnType)
      {
         Parameters = new List<T6Parameter>();
         Name = pName;
         NameSpace = pNameSpace;
         ReturnType = pReturnType;
      }

      public CSharpGenerator.CSharpType ReturnType { get; set; }
      public string Name { get; set; }
      public string NameSpace { get; set; }
      public List<T6Parameter> Parameters { get; set; }
   }
}