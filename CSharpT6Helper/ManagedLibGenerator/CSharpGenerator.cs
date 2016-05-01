using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using CSharpT6Helper.ManagedLibGenerator.Templates;

namespace CSharpT6Helper
{
   internal static class CSharpGenerator
   {
      public const string BaseNs = "Torque6.Engine";

      private static Dictionary<string, List<T6Function>> _namespaceDictionary;
      private static string _outputDir = "Output";

      public static void GenerateFromXmlFile(string XMLFile)
      {
         DirectoryInfo directory = new DirectoryInfo(_outputDir);
         if (directory.Exists)
         {
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            directory.Delete();
         }

         _namespaceDictionary = new Dictionary<string, List<T6Function>>();
         XmlReader reader = XmlReader.Create(XMLFile);
         while (reader.Read())
         {
            // Only detect start elements.
            if (reader.IsStartElement())
            {
               // Get element name and switch on it.
               switch (reader.Name)
               {
                  case "Class":
                     ClassParser.ParseClass(reader.ReadSubtree()).SaveToFile(_outputDir);
                     break;
                  case "Function":
                     T6Function function = FunctionParser.ParseFunction(reader);
                     if(function == null) break;
                     if (!_namespaceDictionary.ContainsKey(function.NameSpace))
                        _namespaceDictionary[function.NameSpace] = new List<T6Function>();
                     _namespaceDictionary[function.NameSpace].Add(function);
                     break;
               }
            }
         }
         reader.Close();
         SaveNamespaceDictionary();
      }

      private static void SaveNamespaceDictionary()
      {
         foreach (KeyValuePair<string, List<T6Function>> keyValuePair in _namespaceDictionary)
         {
            StringBuilder internalsStringBuilder = new StringBuilder();
            StringBuilder functionsStringBuilder = new StringBuilder();

            foreach (T6Function t6Function in keyValuePair.Value)
            {
               internalsStringBuilder.Append(NativeFunctionTemplate.GetFunctionString(keyValuePair.Key, t6Function));
            }

            foreach (T6Function t6Function in keyValuePair.Value)
            {
               string internalName = keyValuePair.Key + "_" + t6Function.Name;
               
               functionsStringBuilder.Append(FunctionTemplate.GetMethodString(internalName, t6Function));
            }

            Directory.CreateDirectory(_outputDir + "/Torque6/Engine/Namespaces");
            StreamWriter SW = new StreamWriter(_outputDir + "/Torque6/Engine/Namespaces/" + keyValuePair.Key + ".cs");

            SW.Write(NamespaceTemplate.GetClassString(keyValuePair.Key, 
               internalsStringBuilder.ToString().Trim(), 
               functionsStringBuilder.ToString().Trim(),
               GetCustomForNamespace(keyValuePair.Key)));
            
            SW.Close();
         }
      }

      private static string GetCustomForNamespace(string pNs)
      {
         switch (pNs)
         {
            case "Sim":
               return @"[StructLayout(LayoutKind.Explicit, Size = 4)]
      public partial struct SimObjectPtr
      {
         [FieldOffset(0)] public IntPtr ObjPtr;
      }
";
         }
         return "";
      }

      public class CSharpType
      {
         private CSharpType(string pType)
         {
            ManagedType = pType;
            NativeReturnType = pType;
            NativeArgType = pType;
         }

         private CSharpType(string pType, string pNativeType)
         {
            ManagedType = pType;
            NativeReturnType = pNativeType;
            NativeArgType = pNativeType;
         }

         private CSharpType(string pType, string pNativeReturnType, string pNativeArgType)
         {
            ManagedType = pType;
            NativeReturnType = pNativeReturnType;
            NativeArgType = pNativeArgType;
         }

         public string ManagedType { get; set; }
         public string NativeReturnType { get; set; }
         public string NativeArgType { get; set; }

         public override string ToString()
         {
            return base.ToString();
         }

         public string GetArg(string localName)
         {
            if (NativeArgType == "IntPtr" && ManagedType != "IntPtr")
            {
               return localName + ".ObjectPtr->ObjPtr";
            }
            if (ManagedType == "SimObject[]")
            {
               return localName + ".Select(x => x.ObjectPtr->ObjPtr).ToArray()";
            }
            return localName;
         }

         public string GetReturn(string expr)
         {
            if (NativeReturnType == "IntPtr" && ManagedType == "string")
            {
               return "Marshal.PtrToStringAnsi(" + expr + ")";
            }
            if (NativeReturnType == "IntPtr" && ManagedType != "IntPtr")
            {
               if (ManagedType == "SimObjectPtr*")
                  return "(SimObjectPtr*)" + expr;
               return "new " + ManagedType + "(" + expr + ")";
            }
            if (NativeReturnType == "IntPtr[]" && ManagedType == "SimObject[]")
               return expr + ".Select(x => new SimObject(x)).ToArray()";
            return expr;
         }

         public static CSharpType ToCSharpType(string pType)
         {
            switch (pType)
            {
               case "Bool":
                  return new CSharpType("bool");
               case "Int":
                  return new CSharpType("int");
               case "IntArray":
                  return new CSharpType("int[]");
               case "UInt":
                  return new CSharpType("uint");
               case "UChar":
                  return new CSharpType("byte");
               case "UShort":
                  return new CSharpType("ushort");
               case "Float":
                  return new CSharpType("float");
               case "Double":
                  return new CSharpType("double");
               case "String":
                  return new CSharpType("string", "IntPtr", "string");
               case "Void":
                  return new CSharpType("void");
               case "Color":
                  return new CSharpType("Color");
               case "Point2I":
                  return new CSharpType("Point2I");
               case "Point2F":
                  return new CSharpType("Point2F");
               case "Point3I":
                  return new CSharpType("Point3I");
               case "Point3F":
                  return new CSharpType("Point3F");
               case "Point4F":
                  return new CSharpType("Point4F");
               case "Box3F":
                  return new CSharpType("Box3F");
               case "RectI":
                  return new CSharpType("RectI");
               case "StringArray":
                  return new CSharpType("string[]");
               case "SimObjectPtr<SimObject>":
                  return new CSharpType("SimObjectPtr*", "IntPtr");
               case "SimObjectPtrArray":
                  return new CSharpType("SimObject[]", "IntPtr[]");
               default:
                  return new CSharpType(pType, "IntPtr");
            }
         }
      }
   }
}