using System.Diagnostics;
using System.Xml;

namespace CSharpT6Helper
{
   internal class FunctionParser
   {
      public static T6Function ParseFunction(XmlReader pReader)
      {
         if (pReader.Name != "Function" || !pReader.IsStartElement())
            return null;
         string Name = pReader["Name"];
         CSharpGenerator.CSharpType ReturnType = CSharpGenerator.CSharpType.ToCSharpType(pReader["ReturnType"]);
         string NameSpace = pReader["NameSpace"];
         T6Function function = new T6Function(Name, NameSpace, ReturnType);
         pReader.Read();
         pReader.Read();
         if (pReader.Name == "Params" && !pReader.IsEmptyElement)
         {
            while (pReader.Read())
            {
               if (!pReader.IsStartElement() && pReader.Name == "Params")
                  break;
               string paramName = pReader["Name"];
               string defaultvalue = pReader["DefaultValue"];
               CSharpGenerator.CSharpType paramType = CSharpGenerator.CSharpType.ToCSharpType(pReader["Type"]);
               T6Parameter parameter = new T6Parameter(paramName, paramType, defaultvalue == "N/A" ? null : defaultvalue);
               function.Parameters.Add(parameter);
            }
         }
         if (function.Name == "WrapObject")
            function.Parameters[0].ParamType = CSharpGenerator.CSharpType.ToCSharpType("IntPtr");
         Debug.Assert(NameSpace != null, "NameSpace != null");
         return function;
      }
   }
}