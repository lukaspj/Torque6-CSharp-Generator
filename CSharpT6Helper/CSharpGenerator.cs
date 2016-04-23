using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CSharpT6Helper
{
   internal static class CSharpGenerator
   {
      private static Dictionary<string, List<T6Function>> NamespaceDictionary;
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

         NamespaceDictionary = new Dictionary<string, List<T6Function>>();
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
                     GenerateClass(reader.ReadSubtree());
                     break;
                  case "Function":
                     GenerateFunction(reader);
                     break;
               }
            }
         }
         reader.Close();
         SaveNamespaceDictionary();
      }

      private static void GenerateFunction(XmlReader pReader)
      {
         if (pReader.Name != "Function" || !pReader.IsStartElement())
            return;
         string Name = pReader["Name"];
         CSharpType ReturnType = CSharpType.ToCSharpType(pReader["ReturnType"]);
         string NameSpace = pReader["NameSpace"];
         T6Function function = new T6Function(Name, ReturnType);
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
               CSharpType paramType = CSharpType.ToCSharpType(pReader["Type"]);
               T6Parameter parameter = new T6Parameter(paramName, paramType, defaultvalue == "N/A" ? null : defaultvalue);
               function.Parameters.Add(parameter);
            }
         }
         if (function.Name == "WrapObject")
            function.Parameters[0].ParamType = CSharpType.ToCSharpType("IntPtr");
         Debug.Assert(NameSpace != null, "NameSpace != null");
         if (!NamespaceDictionary.ContainsKey(NameSpace))
            NamespaceDictionary[NameSpace] = new List<T6Function>();
         NamespaceDictionary[NameSpace].Add(function);
      }

      private static void GenerateClass(XmlReader pReader)
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
                     CSharpType ReturnType = CSharpType.ToCSharpType(pReader["ReturnType"]);
                     T6Function function = new T6Function(Name, ReturnType);
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
                           CSharpType paramType = CSharpType.ToCSharpType(pReader["Type"]);
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
                        Type = CSharpType.ToCSharpType(pReader["Type"]),
                        Out = bool.Parse(pReader["Out"]),
                        Count = int.Parse(pReader["Count"])
                     };
                     pReader.Read();
                     pReader.Read();
                     if (pReader.Name == "Property.Getter")
                     {
                        string propGetterName = pReader["Name"];
                        CSharpType propGetterType = CSharpType.ToCSharpType(pReader["ReturnType"]);
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        string getterName = pReader["Name"];
                        CSharpType getterType = CSharpType.ToCSharpType(pReader["Type"]);
                        pReader.Read();
                        pReader.Read();
                        if (property.Count > 1)
                        {
                           pReader.Read();
                           pReader.Read();
                        }
                        if (propGetterType.ManagedType == "void")
                        {
                           string outParamName = pReader["Name"];
                           CSharpType outParamType = CSharpType.ToCSharpType(pReader["Type"]);
                           pReader.Read();
                           pReader.Read();
                           property.Getter = new T6OutParamGetter(propGetterName, propGetterType, getterName,
                              getterType, outParamName, outParamType)
                           {
                              HasIndex = property.Count > 1
                           };
                        }
                        else
                           property.Getter = new T6Getter(propGetterName, propGetterType, getterName, getterType)
                           {
                              HasIndex = property.Count > 1
                           };

                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                     }
                     if (pReader.Name == "Property.Setter")
                     {
                        string propSetterName = pReader["Name"];
                        CSharpType propReturnType = CSharpType.ToCSharpType(pReader["ReturnType"]);
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        string setterName = pReader["Name"];
                        CSharpType setterType = CSharpType.ToCSharpType(pReader["Type"]);
                        pReader.Read();
                        pReader.Read();
                        if (property.Count > 1)
                        {
                           pReader.Read();
                           pReader.Read();
                        }
                        string setterName2 = pReader["Name"];
                        CSharpType setterType2 = CSharpType.ToCSharpType(pReader["Type"]);
                        property.Type = setterType2;

                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();
                        pReader.Read();

                        T6Setter setter = new T6Setter(propSetterName, propReturnType, setterName, setterType,
                           setterName2, setterType2)
                        {
                           HasIndex = property.Count > 1
                        };
                        property.Setter = setter;
                     }
                     if (property.Getter != null || property.Setter != null)
                        t6Class.Properties.Add(property);
                  }
                  break;
            }
         }
         t6Class.SaveToFile(_outputDir);
      }

      private static void SaveNamespaceDictionary()
      {
         foreach (KeyValuePair<string, List<T6Function>> keyValuePair in NamespaceDictionary)
         {
            StringBuilder internalsStringBuilder = new StringBuilder();
            StringBuilder functionsStringBuilder = new StringBuilder();

            foreach (T6Function t6Function in keyValuePair.Value)
            {
               string paramString = t6Function.Parameters.Aggregate("",
                  (x, y) => x + ", " + y.ParamType.NativeType + " " + y.ParamName);
               if (paramString.Length > 0)
                  paramString = paramString.Substring(2);

               string internalName = keyValuePair.Key + "_" + t6Function.Name;

               internalsStringBuilder.Append(
                  String.Format(@"

         [DllImport(""Torque6_DEBUG"", CallingConvention = CallingConvention.Cdecl)]
         internal static extern {1} {0}({2});", internalName, t6Function.ReturnType.NativeType, paramString));
            }

            foreach (T6Function t6Function in keyValuePair.Value)
            {
               string paramString = "";
               string argString = "";
               foreach (T6Parameter param in t6Function.Parameters)
               {
                  paramString += ", " + param.ParamType.ManagedType + " " +
                                 param.ParamName;
                  argString += ", " + param.ParamType.GetArg(param.ParamName);
               }
               if (paramString.Length > 0)
                  paramString = paramString.Substring(2);
               if (argString.Length > 0)
                  argString = argString.Substring(2);

               string internalName = keyValuePair.Key + "_" + t6Function.Name;

               string returnString =
                  t6Function.ReturnType.GetReturn(String.Format("InternalUnsafeMethods.{0}({1})",
                     internalName, argString));
               if (t6Function.ReturnType.ManagedType != "void")
                  returnString = "return " + returnString;

               functionsStringBuilder.Append(String.Format(@"

      public static {1} {0}({3})
      {{
         {2};
      }}", t6Function.Name, t6Function.ReturnType.ManagedType, returnString, paramString, argString));
            }
            Directory.CreateDirectory(_outputDir + "/Namespaces");
            StreamWriter SW = new StreamWriter(_outputDir + "/Namespaces/" + keyValuePair.Key + ".cs");

            SW.Write(@"using System;
using System.Runtime.InteropServices;
using Torque6_Bridge.SimObjects;
using Torque6_Bridge.Utility;
using Torque6_Bridge.Types;

namespace Torque6_Bridge.Namespaces
{{
   public static unsafe class {0}
   {{
      {3}
      #region UnsafeNativeMethods

      new internal struct InternalUnsafeMethods
      {{
         {1}
      }}

      #endregion
      
      #region Functions

      {2}      

      #endregion
   }}
}}", keyValuePair.Key, internalsStringBuilder.ToString().Trim(), functionsStringBuilder.ToString().Trim(),
               GetCustomForNamespace(keyValuePair.Key));

            SW.Close();
         }
      }

      private static string GetCustomForNamespace(string pNS)
      {
         switch (pNS)
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
            NativeType = pType;
         }

         private CSharpType(string pType, string pNativeType)
         {
            ManagedType = pType;
            NativeType = pNativeType;
         }

         public string ManagedType { get; set; }
         public string NativeType { get; set; }

         public override string ToString()
         {
            return base.ToString();
         }

         public string GetArg(string localName)
         {
            if (NativeType == "IntPtr" && ManagedType != "IntPtr")
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
            if (NativeType == "IntPtr" && ManagedType != "IntPtr")
            {
               if (ManagedType == "SimObjectPtr*")
                  return "(SimObjectPtr*)" + expr;
               return "new " + ManagedType + "(" + expr + ")";
            }
            if (NativeType == "IntPtr[]" && ManagedType == "SimObject[]")
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
                  return new CSharpType("string");
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

   internal class T6Class
   {
      public T6Class()
      {
         Functions = new List<T6Function>();
         Properties = new List<T6Property>();
         ParentList = new List<string>();
      }

      public string Name { get; set; }
      public string Namespace { get; set; }
      public List<string> ParentList { get; set; }
      public List<T6Function> Functions { get; set; }
      public List<T6Property> Properties { get; set; }

      public void SaveToFile(string pOutputDir)
      {
         string functions = GenerateFunctionCodeBlock();
         string properties = GeneratePropertiesCodeBlock();
         string internals = GenerateInternalsCodeBlock();
         string specifics = GenerateSpecificCodeBlock();
         string constructors = GenerateConstructorsCodeBlock();
         string nsPath = Namespace.Replace('.', '/');
         Directory.CreateDirectory(pOutputDir + "/" + nsPath);
         StreamWriter SW = new StreamWriter(pOutputDir + "/" + nsPath + "/" + Name + ".cs");

         string code = String.Format(@"using System;
using System.Linq;
using System.Runtime.InteropServices;
using Torque6_Bridge.Namespaces;
using Torque6_Bridge.Utility;
using Torque6_Bridge.Types;

namespace Torque6_Bridge.{0}
{{
   public unsafe class {1}{2}
   {{
      {7}
      
      #region UnsafeNativeMethods

      new internal struct InternalUnsafeMethods
      {{
         {3}
      }}
      
      #endregion

      #region Properties

      {4}
      
      #endregion
      
      #region Methods

      {5}
      
      #endregion

      {6}
   }}
}}", Namespace, Name, Name != "SimObject" ? " : " + ParentList.Aggregate("", (x, y) => x + ", " + y).Substring(2) : "", internals,
            properties, functions, specifics, constructors);

         if (Name.Equals("SimObject"))
         {
            code =
               code.Replace(" : base(pId)", "")
                  .Replace(" : base(pObjPtr)", "")
                  .Replace(" : base(pName)", "")
                  .Replace(" : base(pObjPtr)", "");
         }

         SW.Write(code);
         SW.Close();
      }

      private string GenerateConstructorsCodeBlock()
      {
         if (Name.Equals("SimObject"))
         {
            return String.Format(@"public {0}()
      {{
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{0}CreateInstance());
      }}

      public {0}(uint pId) : base(pId)
      {{
         ObjectPtr = Sim.FindObjectWrapperById(pId);
      }}

      public {0}(string pName) : base(pName)
      {{
         ObjectPtr = Sim.FindObjectWrapperByName(pName);
      }}

      public {0}(IntPtr pObjPtr) : base(pObjPtr)
      {{
         ObjectPtr = Sim.WrapObject(pObjPtr);
      }}

      public {0}(Sim.SimObjectPtr* pObjPtr) : base(pObjPtr)
      {{
         ObjectPtr = pObjPtr;
      }}

      public {0}(SimObject pObj)
      {{
         ObjectPtr = pObj.ObjectPtr;
      }}", Name);
         }

         return String.Format(@"
      public {0}()
      {{
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{0}CreateInstance());
      }}

      public {0}(uint pId) : base(pId)
      {{
      }}

      public {0}(string pName) : base(pName)
      {{
      }}

      public {0}(IntPtr pObjPtr) : base(pObjPtr)
      {{
      }}

      public {0}(Sim.SimObjectPtr* pObjPtr) : base(pObjPtr)
      {{
      }}

      public {0}(SimObject pObj) : base(pObj)
      {{
      }}", Name);
      }

      private string GenerateSpecificCodeBlock()
      {
         if (Name.Equals("SimObject"))
         {
            return @"#region SimObject specifics

      public T As<T>() where T : new()
      {
         return (T)Activator.CreateInstance(typeof (T), this);
      }

      public Sim.SimObjectPtr* ObjectPtr { get; protected set; }

      public bool IsDead()
      {
         return ObjectPtr->ObjPtr == IntPtr.Zero;
      }

      #region IDisposable

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool pDisposing)
      {
         if (ObjectPtr->ObjPtr != IntPtr.Zero)
         {
            Marshal.FreeHGlobal((IntPtr) ObjectPtr);
         }
      }

      ~SimObject()
      {
         Dispose(false);
      }

      #endregion

            #endregion";
         }
         return "";
      }

      private string GenerateInternalsCodeBlock()
      {
         StringBuilder SB = new StringBuilder();
         foreach (T6Property t6Property in Properties)
         {
            string get = "";
            if (t6Property.Getter != null)
               get =
                  String.Format(@"         [DllImport(""Torque6_DEBUG"", CallingConvention = CallingConvention.Cdecl)]
         internal static extern {1} {0}({2});

", t6Property.Getter.PropGetterName, t6Property.Getter.PropReturnType.NativeType, t6Property.Getter.GetParamString());

            string set = "";
            if (t6Property.Setter != null)
               set =
                  String.Format(@"         [DllImport(""Torque6_DEBUG"", CallingConvention = CallingConvention.Cdecl)]
         internal static extern void {0}({1});

", t6Property.Setter.PropSetterName, t6Property.Setter.GetParamString());
            SB.Append(get + set);
         }

         foreach (T6Function t6Function in Functions)
         {
            string paramString = t6Function.Parameters.Aggregate("",
               (x, y) => x + ", " + y.ParamType.NativeType + " " + y.ParamName);
            if (paramString.Length > 0)
               paramString = paramString.Substring(2);
            SB.Append(
               String.Format(@"         [DllImport(""Torque6_DEBUG"", CallingConvention = CallingConvention.Cdecl)]
         internal static extern {1} {0}({2});

", t6Function.Name, t6Function.ReturnType.NativeType, paramString));
         }
         return SB.ToString().Trim();
      }

      private string GeneratePropertiesCodeBlock()
      {
         StringBuilder SB = new StringBuilder();
         foreach (T6Property t6Property in Properties)
         {
            if (t6Property.Count > 1)
            {
               string fieldVectorType = "";
               if (t6Property.Type.ManagedType.Equals("string")
                   || t6Property.Type.ManagedType.Equals("int")
                   || t6Property.Type.ManagedType.Equals("float")
                   || t6Property.Type.ManagedType.Equals("double")
                   || t6Property.Type.ManagedType.Equals("char"))
                  fieldVectorType = "Primitive";
               string append = String.Format(@"
      public {5}FieldVector<{0}> {1}
      {{
         get
         {{
            return new {5}FieldVector<{0}>(this, {2}, InternalUnsafeMethods.{3},
               InternalUnsafeMethods.{4});
         }}
      }}
", t6Property.Type.ManagedType, t6Property.Name, t6Property.Count, t6Property.Getter.PropGetterName,
                  t6Property.Setter.PropSetterName, fieldVectorType);

               SB.Append(append);
            }
            else
            {
               string append = String.Format(@"      public {1} {0}
      {{
", t6Property.Name, t6Property.Type.ManagedType);

               string get = "";
               if (t6Property.Getter != null)
               {
                  if (t6Property.Out)
                  {
                     get = String.Format(@"         get
         {{
            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
            {1} outVal;
            InternalUnsafeMethods.{0}(ObjectPtr->ObjPtr, out outVal);
            return outVal;
         }}
", t6Property.Getter.PropGetterName, t6Property.Type.ManagedType);
                  }
                  else
                  {
                     get = String.Format(@"         get
         {{
            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
            return {0};
         }}
",
                        t6Property.Type.GetReturn(String.Format("InternalUnsafeMethods.{0}(ObjectPtr->ObjPtr)",
                           t6Property.Getter.PropGetterName)));
                  }
               }

               string set = "";
               if (t6Property.Setter != null)
                  set = String.Format(@"         set
         {{
            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
            InternalUnsafeMethods.{0}(ObjectPtr->ObjPtr, {1});
         }}", t6Property.Setter.PropSetterName, t6Property.Type.GetArg("value"));

               append += get;
               append += set;
               append += @"
      }
";
               SB.Append(append);
            }
         }
         return SB.ToString().Trim();
      }

      private string GenerateFunctionCodeBlock()
      {
         StringBuilder SB = new StringBuilder();
         foreach (T6Function t6Function in Functions)
         {
            if (t6Function.Parameters.Count < 1)
               continue;
            string paramString = "";
            string argString = "";
            for (int i = 1; i < t6Function.Parameters.Count; i++)
            {
               paramString += ", " + t6Function.Parameters[i].ParamType.ManagedType + " " +
                              t6Function.Parameters[i].ParamName;
               argString += ", " + t6Function.Parameters[i].ParamType.GetArg(t6Function.Parameters[i].ParamName);
               if (t6Function.Parameters[i].ParamDefault != null)
                  paramString += " = " + t6Function.Parameters[i].ParamDefault;
            }
            if (paramString.Length > 0)
               paramString = paramString.Substring(2);
            string retString =
               t6Function.ReturnType.GetReturn(String.Format("InternalUnsafeMethods.{0}{1}(ObjectPtr->ObjPtr{2})", Name,
                  t6Function.Name.Substring(Name.Length), argString));
            if (t6Function.ReturnType.ManagedType != "void")
               retString = "return " + retString;
            SB.Append(String.Format(@"      public {2} {0}({1})
      {{
         if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
         {3};
      }}

", t6Function.Name.Substring(Name.Length), paramString, t6Function.ReturnType.ManagedType, retString));
         }
         return SB.ToString().Trim();
      }
   }

   internal class T6Setter
   {
      public T6Setter(string pPropSetterName, CSharpGenerator.CSharpType pPropReturnType, string pSetterName,
         CSharpGenerator.CSharpType pSetterType,
         string pSetterName2, CSharpGenerator.CSharpType pSetterType2)
      {
         PropSetterName = pPropSetterName;
         PropReturnType = pPropReturnType;
         SetterName = pSetterName;
         SetterType = pSetterType;
         SetterName2 = pSetterName2;
         SetterType2 = pSetterType2;
         HasIndex = false;
      }

      public string PropSetterName { get; set; }
      public CSharpGenerator.CSharpType PropReturnType { get; set; }
      public string SetterName { get; set; }
      public CSharpGenerator.CSharpType SetterType { get; set; }
      public string SetterName2 { get; set; }
      public CSharpGenerator.CSharpType SetterType2 { get; set; }
      public bool HasIndex { get; set; }

      public virtual string GetParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return SetterType.NativeType + " " + SetterName + indexString
                + ", " + SetterType2.NativeType + " " + SetterName2;
      }
   }

   internal class T6Getter
   {
      public T6Getter(string pPropGetterName, CSharpGenerator.CSharpType pPropReturnType, string pGetterName,
         CSharpGenerator.CSharpType pGetterType)
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
      public List<T6Parameter> Parameters { get; set; }
      public bool HasIndex { get; set; }

      public virtual string GetParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return GetterType.NativeType + " " + GetterName + indexString;
      }
   }

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
         return base.GetParamString() + ", out " + OutParamType.NativeType + " " + OutParamName;
      }
   }

   internal class T6Property
   {
      public T6Property(string pPropertyName, int pCount = -1, bool pOut = false)
      {
         Name = pPropertyName;
         Count = pCount;
         Out = pOut;
      }

      public string Name { get; set; }
      public CSharpGenerator.CSharpType Type { get; set; }
      public int Count { get; set; }
      public bool Out { get; set; }
      public T6Getter Getter { get; set; }
      public T6Setter Setter { get; set; }
   }

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

   internal class T6Function
   {
      public T6Function(string pName, CSharpGenerator.CSharpType pReturnType)
      {
         Parameters = new List<T6Parameter>();
         Name = pName;
         ReturnType = pReturnType;
      }

      public CSharpGenerator.CSharpType ReturnType { get; set; }
      public string Name { get; set; }
      public List<T6Parameter> Parameters { get; set; }
   }
}