using System.Xml;

namespace CSharpT6Helper
{
   internal class ClassParser
   {
      public static T6Class ParseClass(XmlReader pReader)
      {
         T6Class t6Class = new T6Class();
         while (pReader.Read())
         {
            if (!pReader.IsStartElement()) continue;
            switch (pReader.Name)
            {
               case "Class":
                  t6Class.Name = pReader["Name"];
                  t6Class.Namespace = pReader["Namespace"];
                  break;
               case "Class.Parents":
                  while (pReader.Read())
                  {
                     if (!pReader.IsStartElement() && pReader.Name == "Class.Parents")
                        break;
                     t6Class.ParentList.Add(pReader["Name"]);
                  }
                  break;
               case "Functions":
                  while (pReader.Read())
                  {
                     if (!pReader.IsStartElement() && pReader.Name == "Functions")
                        break;
                     if (pReader.Name != "Function" || !pReader.IsStartElement())
                        continue;
                     string Name = pReader["Name"];
                     CSharpGenerator.CSharpType ReturnType = CSharpGenerator.CSharpType.ToCSharpType(pReader["ReturnType"]);
                     T6Function function = new T6Function(Name, null, ReturnType);
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
                     t6Class.Functions.Add(function);
                  }
                  break;
               case "Properties":
                  if (pReader.IsEmptyElement)
                     continue;
                  while (pReader.Read())
                  {
                     if (!pReader.IsStartElement() && pReader.Name == "Properties")
                        break;
                     if (pReader.IsEmptyElement)
                        continue;
                     string PropertyName = pReader["Name"];

                     T6Property property = new T6Property(PropertyName)
                     {
                        Type = CSharpGenerator.CSharpType.ToCSharpType(pReader["Type"]),
                        Out = bool.Parse(pReader["Out"]),
                        Count = int.Parse(pReader["Count"])
                     };
                     pReader.Read();
                     pReader.Read();
                     if (pReader.Name == "Property.Getter")
                     {
                        string propGetterName = pReader["Name"];
                        CSharpGenerator.CSharpType propGetterType = CSharpGenerator.CSharpType.ToCSharpType(pReader["ReturnType"]);
                        property.Getter = new T6Getter(propGetterName, propGetterType, null, null);
                        pReader.Read();
                        pReader.Read();

                        while (pReader.Read())
                        {
                           if (!pReader.IsStartElement() && pReader.Name == "Getter.Params")
                              break;
                           string paramName = pReader["Name"];
                           string defaultvalue = pReader["DefaultValue"];
                           CSharpGenerator.CSharpType paramType = CSharpGenerator.CSharpType.ToCSharpType(pReader["Type"]);
                           T6Parameter parameter = new T6Parameter(paramName, paramType, defaultvalue == "N/A" ? null : defaultvalue);
                           property.Getter.Parameters.Add(parameter);
                        }

                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                     }
                     if (pReader.Name == "Property.Setter")
                     {
                        string propSetterName = pReader["Name"];
                        CSharpGenerator.CSharpType propSetterType = CSharpGenerator.CSharpType.ToCSharpType(pReader["ReturnType"]);
                        property.Setter = new T6Setter(propSetterName, propSetterType, null, null, null, null);
                        pReader.Read();
                        pReader.Read();

                        while (pReader.Read())
                        {
                           if (!pReader.IsStartElement() && pReader.Name == "Setter.Params")
                              break;
                           string paramName = pReader["Name"];
                           string defaultvalue = pReader["DefaultValue"];
                           CSharpGenerator.CSharpType paramType = CSharpGenerator.CSharpType.ToCSharpType(pReader["Type"]);
                           T6Parameter parameter = new T6Parameter(paramName, paramType, defaultvalue == "N/A" ? null : defaultvalue);
                           property.Setter.Parameters.Add(parameter);
                        }

                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                     }
                     if (property.Getter != null || property.Setter != null)
                        t6Class.Properties.Add(property);
                  }
                  break;
            }
         }
         return t6Class;
      }
   }
}