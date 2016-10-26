using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CSharpT6Helper.ManagedLibGenerator;
using CSharpT6Helper.ManagedLibGenerator.Templates;

namespace CSharpT6Helper
{
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
         string constructors = ConstructorsTemplate.GetConstructorsString(Name);
         string ns = CSharpGenerator.BaseNs;
         if (!Namespace.Equals(""))
         {
            ns += "." + Namespace;
         }
         string nsPath = ns.Replace('.', '/');
         Directory.CreateDirectory(pOutputDir + "/" + nsPath);
         StreamWriter SW = new StreamWriter(pOutputDir + "/" + nsPath + "/" + Name + ".cs");

         List<string> parentList = Name != "SimObject" ? ParentList : null;

         string code = SimObjectTemplate.GetClassString(ns, Name, parentList, constructors, internals, properties, functions, specifics);

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

      public void SetPointerFromObject(SimObject obj)
      {
         ObjectPtr = obj.ObjectPtr;
      }

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
            {
               get = NativeFunctionTemplate.GetFunctionString(t6Property.Getter);
            }
            
            string set = "";
            if (t6Property.Setter != null)
            {
               set = NativeFunctionTemplate.GetFunctionString(t6Property.Setter);
            }

            SB.Append(get + set);
         }

         foreach (T6Function t6Function in Functions)
         {
            SB.Append(NativeFunctionTemplate.GetFunctionString(t6Function));
         }
         return SB.ToString().Trim();
      }

      private string GeneratePropertiesCodeBlock()
      {
         StringBuilder SB = new StringBuilder();
         foreach (T6Property t6Property in Properties)
         {
            CSharpGenerator.CSharpType propType = t6Property.Getter?.GetterType ?? t6Property.Setter?.ValueParameter.ParamType;

            if (t6Property.Out)
            {
               propType = (t6Property.Getter as T6OutParamGetter)?.OutParam.ParamType ?? t6Property.Setter?.ValueParameter.ParamType;
            }

            if (t6Property.Count > 1)
            {
               string fieldVectorType = "";
               string typeArg = $"<{t6Property.Type.ManagedType}>";
               if (t6Property.Type.ManagedType.Equals("int")
                   || t6Property.Type.ManagedType.Equals("float")
                   || t6Property.Type.ManagedType.Equals("double")
                   || t6Property.Type.ManagedType.Equals("char"))
                  fieldVectorType = "Primitive";
               if (t6Property.Type.ManagedType.Equals("string"))
               {
                  fieldVectorType = "String";
                  typeArg = "";
               }
               string append = String.Format(@"
      public {5}FieldVector{0} {1}
      {{
         get
         {{
            return new {5}FieldVector{0}(this, {2}, InternalUnsafeMethods.{3},
               InternalUnsafeMethods.{4});
         }}
      }}
", typeArg, t6Property.Name, t6Property.Count, t6Property.Getter.GetterName,
                  t6Property.Setter.SetterName, fieldVectorType);

               SB.Append(append);
            }
            else
            {
               string append = String.Format(@"      public {1} {0}
      {{
", t6Property.Name, propType.ManagedType);

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
", t6Property.Getter.GetterName, propType.ManagedType);
                  }
                  else
                  {
                     get = String.Format(@"         get
         {{
            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
            return {0};
         }}
",
                        propType.GetReturn(String.Format("InternalUnsafeMethods.{0}(ObjectPtr->ObjPtr)",
                           t6Property.Getter.GetterName)));
                  }
               }

               string set = "";
               if (t6Property.Setter != null)
                  set = String.Format(@"         set
         {{
            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();
            InternalUnsafeMethods.{0}(ObjectPtr->ObjPtr, {1});
         }}", t6Property.Setter.SetterName, propType.GetArg("value"));

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
}