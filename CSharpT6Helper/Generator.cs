using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpT6Helper
{
   internal static class Generator
   {
      public static string GenerateCInterface(string pText)
      {
         StringReader SR = new StringReader(pText);
         StringBuilder SB = new StringBuilder();
         string line;

         string className = "";
         while ((line = SR.ReadLine()) != null)
         {
            if (LineIsMethodHeader(line))
            {
               className = ExtractMethodBody(SB, line, SR);
            }
         }

         SB.Insert(0, String.Format(@"   DLL_PUBLIC {0}* {0}CreateInstance()
   {{
      return new {0}();
   }}

", className));

         return SB.ToString();
      }

      public static string GenerateCSharpInterface(string pText)
      {
         StringReader SR = new StringReader(pText);
         StringBuilder InternalSB = new StringBuilder();
         StringBuilder PublicSB = new StringBuilder();
         StringBuilder PropSB = new StringBuilder();
         string line;

         PropSB.AppendLine("      #region Properties");
         PropSB.AppendLine();

         PublicSB.AppendLine("      #region Methods");
         PublicSB.AppendLine();

         string className = "";
         while ((line = SR.ReadLine()) != null)
         {
            if (LineIsMethodHeader(line))
            {
               className = ConstructCSharpMethod(InternalSB, PublicSB, PropSB, line, SR);
            }
         }

         InternalSB.Insert(0, String.Format(@"      #region UnsafeNativeMethods

      new internal struct InternalUnsafeMethods
      {{
         [DllImport(""Torque6_DEBUG"", CallingConvention = CallingConvention.Cdecl)]
         internal static extern IntPtr {0}CreateInstance();

", className));

         StringBuilder collectiveBuilder = new StringBuilder();

         collectiveBuilder.AppendLine(String.Format(@"   public unsafe class {0} : SimObject
   {{
      public {0}()
      {{
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{0}CreateInstance());
      }}

      public {0}(uint pId) : base(pId)
      {{
      }}

      public {0}(IntPtr pObjPtr) : base(pObjPtr)
      {{
      }}

      public {0}(string pName) : base(pName)
      {{
      }}

      public {0}(Sim.SimObjectPtr* pObjPtr) : base(pObjPtr)
      {{
      }}", className));

         collectiveBuilder.Append(InternalSB);
         collectiveBuilder.AppendLine("      }");
         collectiveBuilder.AppendLine();
         collectiveBuilder.AppendLine("      #endregion");
         collectiveBuilder.AppendLine();

         collectiveBuilder.Append(PropSB);
         collectiveBuilder.AppendLine("      #endregion");
         collectiveBuilder.AppendLine();

         collectiveBuilder.Append(PublicSB);
         collectiveBuilder.AppendLine("      #endregion");
         collectiveBuilder.AppendLine();

         collectiveBuilder.AppendLine("   }");

         return collectiveBuilder.ToString();
      }

      private static string ConstructCSharpMethod(StringBuilder pInternalSB, StringBuilder pPublicSB,
         StringBuilder pPropSB, string line, StringReader SR)
      {
         Match matches =
            Regex.Matches(line,
               "([a-zA-Z\\^\\*:0-9]+).+IJWLayer::([a-zA-Z0-9]*)::([a-zA-Z0-9]*)(?:::([a-zA-Z0-9]*))?\\(((?:(?:[a-zA-Z\\^\\* 0-9]+),)|(?:[a-zA-Z\\^\\* 0-9]+))*\\)")
               [0];
         string returnType = ConvertToCSharpType(matches.Groups[1].Value);
         string className = matches.Groups[2].Value;
         string methodName = matches.Groups[3].Value[0].ToString().ToUpper() + matches.Groups[3].Value.Substring(1);
         string getterSetter = null;
         if (matches.Groups[4].Success)
         {
            getterSetter = matches.Groups[4].Value[0].ToString().ToUpper() + "et";
         }
         string[] parameters = new string[matches.Groups[5].Captures.Count];
         string[] args = new string[matches.Groups[5].Captures.Count];
         int index = 0;
         foreach (Capture capture in matches.Groups[5].Captures)
         {
            string[] paramCap = capture.Value.Trim().Split(' ');
            paramCap[0] = ConvertToCSharpType(paramCap[0]);
            parameters[index] = paramCap[0] + " " + paramCap[1];
            args[index] = paramCap[1];
            index++;
         }

         int indentation = 0;
         while ((line = SR.ReadLine()) != null)
         {
            if (line.Contains("{"))
               indentation++;
            if (line.Contains("}"))
               indentation--;
            if (indentation == 0)
               break;
         }

         if (getterSetter != null)
         {
            if (getterSetter == "Set")
               return className;

            pPropSB.AppendLine(String.Format("      public {0} {1}", returnType, methodName));
            pPropSB.AppendLine("      {");
            pPropSB.AppendLine("         get");
            pPropSB.AppendLine("         {");
            pPropSB.AppendLine("            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();");
            pPropSB.AppendLine(String.Format("            return InternalUnsafeMethods.{0}Get{1}(ObjectPtr->ObjPtr);",
               className, methodName));
            pPropSB.AppendLine("         }");
            pPropSB.AppendLine("         set");
            pPropSB.AppendLine("         {");
            pPropSB.AppendLine("            if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();");
            pPropSB.AppendLine(String.Format("            InternalUnsafeMethods.{0}Set{1}(ObjectPtr->ObjPtr, value);",
               className, methodName));
            pPropSB.AppendLine("         }");
            pPropSB.AppendLine("      }");
            pPropSB.AppendLine();

            pInternalSB.AppendLine("      [DllImport(\"Torque6_DEBUG\", CallingConvention = CallingConvention.Cdecl)]");
            pInternalSB.AppendLine(String.Format("      internal static extern {0} {1}Get{2}(IntPtr pObjectPtr);",
               returnType, className, methodName));
            pInternalSB.AppendLine();
            pInternalSB.AppendLine("      [DllImport(\"Torque6_DEBUG\", CallingConvention = CallingConvention.Cdecl)]");
            pInternalSB.AppendLine(
               String.Format("      internal static extern void {1}Set{2}(IntPtr pObjectPtr, {0} val);", returnType,
                  className, methodName));
            pInternalSB.AppendLine();
         }
         else
         {
            string parameterString = parameters.Aggregate("", (current, parameter) => current + (", " + parameter));
            string argumentString = args.Aggregate("", (current, parameter) => current + (", " + parameter));
            pPublicSB.AppendLine(String.Format("      public {0} {1}({2})", returnType, methodName,
               parameterString != "" ? parameterString.Substring(2) : ""));
            pPublicSB.AppendLine("      {");
            pPublicSB.AppendLine("      	if (IsDead()) throw new Exceptions.SimObjectPointerInvalidException();");
            pPublicSB.AppendLine(String.Format("      	{3}InternalUnsafeMethods.{0}{1}(ObjectPtr->ObjPtr{2});",
               className, methodName, argumentString, returnType == "void" ? "" : "return "));
            pPublicSB.AppendLine("      }");
            pPublicSB.AppendLine();

            pInternalSB.AppendLine("      [DllImport(\"Torque6_DEBUG\", CallingConvention = CallingConvention.Cdecl)]");
            pInternalSB.AppendLine(String.Format("      internal static extern {0} {1}{2}(IntPtr pObjectPtr{3});",
               returnType, className, methodName, parameterString));
            pInternalSB.AppendLine();
         }
         return className;
      }

      private static string ExtractMethodBody(StringBuilder SB, string line, StringReader SR)
      {
         string className;
         SB.AppendLine("   " + ConvertMethodHeader(line, out className));
         int indentation = 0;
         while ((line = SR.ReadLine()) != null)
         {
            if (line.Contains("{"))
               indentation++;
            if (line.Contains("}"))
               indentation--;
            SB.AppendLine("   " + line);
            if (indentation == 0)
               break;
         }
         SB.AppendLine();
         return className;
      }

      private static string ConvertMethodHeader(string pLine, out string pClassName)
      {
         Match matches =
            Regex.Matches(pLine,
               "([a-zA-Z\\^\\*:0-9]+).+IJWLayer::([a-zA-Z0-9]*)::([a-zA-Z0-9]*)(?:::([a-zA-Z0-9]*))?\\(((?:(?:[a-zA-Z\\^\\* <>_:0-9]+),)|(?:[a-zA-Z\\^\\* <>_:0-9]+))*\\)")
               [0];
         string returnType = ConvertType(matches.Groups[1].Value);
         pClassName = matches.Groups[2].Value;
         string methodName = matches.Groups[3].Value[0].ToString().ToUpper() + matches.Groups[3].Value.Substring(1);
         string getterSetter = "";
         if (matches.Groups[4].Success)
         {
            getterSetter = matches.Groups[4].Value[0].ToString().ToUpper() + "et";
         }
         List<string> parameters = new List<string>(matches.Groups[5].Captures.Count);
         int index = 0;
         foreach (Capture capture in matches.Groups[5].Captures)
         {
            string[] paramCap = capture.Value.Trim().Split(' ');
            paramCap[0] = ConvertType(paramCap[0]);
            parameters.Add(paramCap[0] + " " + paramCap[1]);
            index++;
         }
         if (returnType.Equals("CInterface::ColorParam"))
         {
            parameters.Add("CInterface::ColorParam* outColor");
            returnType = "void";
         }
         else if (returnType.Equals("CInterface::Point2IParam")
                  || returnType.Equals("CInterface::Point2FParam")
                  || returnType.Equals("CInterface::Point3IParam")
                  || returnType.Equals("CInterface::Point3FParam"))
         {
            parameters.Add(returnType + "* outPoint");
            returnType = "void";
         }
         string parameterString = parameters.Aggregate("", (current, parameter) => current + (", " + parameter));
         string result = String.Format("DLL_PUBLIC {0} {1}{2}{3}({1}* thisObj{4})", returnType, pClassName, getterSetter,
            methodName, parameterString);

         return result;
      }

      private static string ConvertType(string pType)
      {
         switch (pType)
         {
            case "String^":
               return "const char*";
            case "IJWLayer::Color^":
            case "Color^":
               return "CInterface::ColorParam";
            case "IJWLayer::Point2I^":
            case "Point2I^":
               return "CInterface::Point2IParam";
            case "IJWLayer::Point2F^":
            case "Point2F^":
               return "CInterface::Point2FParam";
            case "IJWLayer::Point3I^":
            case "Point3I^":
               return "CInterface::Point3IParam";
            case "IJWLayer::Point3F^":
            case "Point3F^":
               return "CInterface::Point3FParam";
            default:
               int prefixLength = "IJWLayer::".Length;
               if (pType.StartsWith("IJWLayer::"))
                  pType = pType.Substring(prefixLength, pType.Length - prefixLength - 1) + "*";
               return pType;
         }
      }

      private static string ConvertToCSharpType(string pType)
      {
         if (pType == "String^")
            return "string";
         if (pType.StartsWith("IJWLayer::"))
            pType = "IntPtr";
         return pType;
      }

      private static bool LineIsMethodHeader(string pLine)
      {
         int nextSpace = pLine.IndexOf(" ", StringComparison.Ordinal);
         if (nextSpace <= 0)
            return false;
         string start = pLine.Substring(0, nextSpace);
         if (start.Equals("void")
             || start.Equals("String^")
             || start.Equals("bool")
             || start.Equals("int")
             || start.Equals("float")
             || start.StartsWith("Color")
             || start.StartsWith("IJWLayer::"))
         {
            return true;
         }
         return false;
      }
   }
}