using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace CSharpT6Helper
{
   internal class TSClassMetaData
   {
      private static readonly StringBuilder log = new StringBuilder();
      private static readonly List<string> consoleMethodList = new List<string>();
      private static readonly List<string> consoleFunctionList = new List<string>();
      private static readonly List<string> overloadedFunctionList = new List<string>();
      public DefaultValueCollection DefaultValues;
      public List<string> IgnoredFunctionList = new List<string>();
      public List<string> IgnoredMethodList = new List<string>();
      public NamespaceCollection Namespaces;

      public TSClassMetaData()
      {
         log.Clear();
         consoleMethodList.Clear();
         consoleFunctionList.Clear();
         overloadedFunctionList.Clear();
         LoadSettings();
      }

      public List<CFunction> Functions { get; set; }
      public List<EngineClass> Classes { get; set; }
      public List<T6Field> Fields { get; set; }

      private void LoadSettings()
      {
         IgnoredMethodList.Clear();
         IgnoredFunctionList.Clear();
         if (File.Exists("ignoredMethods.txt"))
         {
            StreamReader SR = new StreamReader("ignoredMethods.txt");
            while (!SR.EndOfStream)
            {
               IgnoredMethodList.Add(SR.ReadLine());
            }
            SR.Close();
         }
         if (File.Exists("ignoredFunctions.txt"))
         {
            StreamReader SR = new StreamReader("ignoredFunctions.txt");
            while (!SR.EndOfStream)
            {
               // ReSharper disable once PossibleNullReferenceException
               string line = SR.ReadLine().Trim();
               if (!line.StartsWith("//"))
                  IgnoredFunctionList.Add(line);
            }
            SR.Close();
         }
         if (File.Exists("objectNamespaces.data"))
         {
            StreamReader SR = new StreamReader("objectNamespaces.data");
            Namespaces = new NamespaceCollection(new StringReader(SR.ReadToEnd()), 0, null);
            SR.Close();
         }
         if (File.Exists("defaultValues.data"))
         {
            StreamReader SR = new StreamReader("defaultValues.data");
            DefaultValues = new DefaultValueCollection(new StringReader(SR.ReadToEnd()));
            SR.Close();
         }
      }

      public TSClassMetaData MergeWith(TSClassMetaData TsClassMetaData)
      {
         return new TSClassMetaData
         {
            Functions = Functions.Concat(TsClassMetaData.Functions).ToList(),
            Classes = Classes.Concat(TsClassMetaData.Classes).ToList(),
            Fields = Fields.Concat(TsClassMetaData.Fields).ToList()
         };
      }

      public static TSClassMetaData GenerateFromBindingFile(string pFile)
      {
         string headerFile = pFile.Substring(0, pFile.Length - "_Binding.h".Length) + ".h";
         string ccFile = pFile.Substring(0, pFile.Length - "_Binding.h".Length) + ".cc";
         if (!File.Exists(ccFile))
            ccFile = pFile.Substring(0, pFile.Length - "_Binding.h".Length) + ".cpp";
         if (!File.Exists(ccFile)
             || !File.Exists(headerFile))
            throw new NotImplementedException();

         List<CFunction> functions = ParseCFunctionsFromBindingFile(pFile);
         List<EngineClass> classes = ParseClassesFromHeaderFile(headerFile, functions);
         List<T6Field> fields = ParseFieldsFromCCFile(ccFile, classes, functions);
         return new TSClassMetaData
         {
            Functions = functions,
            Classes = classes,
            Fields = fields
         };
      }

      private static List<T6Field> ParseFieldsFromCCFile(string pCcFile, List<EngineClass> pClasses,
         List<CFunction> pFunctions)
      {
         List<T6Field> fields = new List<T6Field>();
         StreamReader SR = new StreamReader(pCcFile);
         while (!SR.EndOfStream)
         {
            string line = SR.ReadLine().Trim();
            if (line.Contains("::initPersistFields()")
                && !line.Contains("Parent::initPersistFields()"))
            {
               Match match =
                  Regex.Matches(line,
                     "void\\s+([a-zA-Z 0-9:_]+)::initPersistFields()")
                     [0];
               string className = match.Groups[1].Value;
               EngineClass engineClass = pClasses.Find(c => c.ClassName.Equals(className));

               int indentation = 0;
               while ((line = SR.ReadLine()) != null)
               {
                  line = line.Trim();
                  if (line.StartsWith("//"))
                     continue;
                  if (line.Contains("}"))
                     indentation--;
                  if (indentation != 0)
                  {
                     MatchCollection matches =
                        Regex.Matches(line,
                           "Field\\(\\s*\"?([a-zA-Z0-9:_]+)\"?[\\s,]+([a-zA-Z0-9:_]+)[\\s,]+Offset\\([^\\(\\)]*\\)[\\s,]*([0-9]+)?");
                     if (matches.Count <= 0)
                        continue;
                     match = matches[0];
                     string fieldName = match.Groups[1].Value.Trim();
                     T6Field t6Field = new T6Field
                     {
                        FieldName = fieldName[0].ToString().ToUpper() + fieldName.Substring(1),
                        FieldTypeInfo = T6Field.TypeFromString(match.Groups[2].Value.Trim())
                     };
                     t6Field.FieldCount = match.Groups[3].Success ? int.Parse(match.Groups[3].Value.Trim()) : -1;
                     engineClass.Fields.Add(t6Field);

                     foreach (CFunction cFunction in pFunctions)
                     {
                        if (cFunction.OwnerClass != engineClass) continue;
                        if (
                           cFunction.FunctionName.ToLower()
                              .Equals(engineClass.ClassName.ToLower() + "get" + t6Field.FieldName.ToLower()))
                        {
                           t6Field.GetterFunction = cFunction;
                           cFunction.PropertyFunction = true;
                        }
                        else if (
                           cFunction.FunctionName.ToLower()
                              .Equals(engineClass.ClassName.ToLower() + "set" + t6Field.FieldName.ToLower()))
                        {
                           t6Field.SetterFunction = cFunction;
                           cFunction.PropertyFunction = true;
                        }
                     }

                     fields.Add(t6Field);
                  }
                  if (line.Contains("{"))
                     indentation++;
                  if (indentation == 0)
                     break;
               }
            }
         }
         SR.Close();
         return fields;
      }

      private static List<EngineClass> ParseClassesFromHeaderFile(string pHeaderFile, List<CFunction> pFunctions)
      {
         List<EngineClass> classes = new List<EngineClass>();
         StreamReader SR = new StreamReader(pHeaderFile);
         while (!SR.EndOfStream)
         {
            string line = SR.ReadLine().Trim();
            if (line.StartsWith("class") && line.Contains(":") && !line.Contains("##"))
            {
               MatchCollection parentMatches =
                  Regex.Matches(line,
                     ":?\\s*,?\\s*public\\s+(?:virtual\\s+)?([a-zA-Z0-9:<>]+)");
               MatchCollection classMatches =
                  Regex.Matches(line,
                     "class (?:[a-zA-Z 0-9:_]* )?([a-zA-Z0-9:]+)\\s*:");
               if (classMatches.Count <= 0)
                  continue;

               EngineClass engineClass = new EngineClass(classMatches[0].Groups[1].Value);
               foreach (Match match in parentMatches)
               {
                  engineClass.ParentClassNames.Add(match.Groups[1].Value);
               }

               foreach (CFunction cFunction in pFunctions)
               {
                  if (cFunction.FunctionName.StartsWith(engineClass.ClassName)
                      && cFunction.FunctionParams.Count > 0
                      && cFunction.FunctionParams[0].ParamTypeInfo.DataName != null
                      && cFunction.FunctionParams[0].ParamTypeInfo.DataName.Equals(engineClass.ClassName)
                      || cFunction.FunctionName == engineClass.ClassName + "CreateInstance")
                     cFunction.OwnerClass = engineClass;
               }

               classes.Add(engineClass);

               StringBuilder bodyBuilder = new StringBuilder();
               int indentation = 0;
               while ((line = SR.ReadLine()) != null)
               {
                  if (line.Contains("}"))
                     indentation--;
                  if (indentation != 0)
                     bodyBuilder.AppendLine(line);
                  if (line.Contains("{"))
                     indentation++;
                  if (indentation == 0)
                     break;
               }
            }
         }
         SR.Close();
         return classes;
      }

      private static List<CFunction> ParseCFunctionsFromBindingFile(string pFile)
      {
         List<CFunction> functions = new List<CFunction>();
         StreamReader SR = new StreamReader(pFile);
         while (!SR.EndOfStream)
         {
            string line = SR.ReadLine().Trim();
            if (line.StartsWith("ConsoleMethod(")
                || line.StartsWith("ConsoleMethodWithDocs(")
                || line.StartsWith("ConsoleStaticMethod(")
                || line.StartsWith("ConsoleStaticMethodWithDocs(")
                || line.StartsWith("ConsoleFunction(")
                || line.StartsWith("ConsoleFunctionWithDocs(")
                || line.StartsWith("ConsoleNamespaceFunction("))
            {
               Match matches =
                  Regex.Matches(line,
                     "Console(?:Static|Namespace)?(Method|Function)(?:WithDocs)?\\s*\\(\\s*(?:([a-zA-Z0-9_]+)[\\s,]+)?([a-zA-Z0-9_:\\* ]+)\\s*,\\s*(?:[a-zA-Z_][\\sa-zA-Z0-9:_ \\*]+\\s*,)?\\s*([0-9]+)\\s*,\\s*([0-9a-zA-Z: +]+)")
                     [0];
               string functionName = null;
               if (matches.Groups[1].Value.Equals("Method"))
               {
                  functionName = matches.Groups[3].Value;
                  functionName = functionName[0].ToString().ToUpper() + functionName.Substring(1);
                  functionName = matches.Groups[2].Value + functionName;
                  consoleMethodList.Add(functionName);
               }
               if (matches.Groups[1].Value.Equals("Function"))
               {
                  if (line.StartsWith("ConsoleNamespaceFunction("))
                  {
                     functionName = matches.Groups[3].Value;
                     functionName = functionName[0].ToString().ToUpper() + functionName.Substring(1);
                     consoleFunctionList.Add(functionName);
                  }
                  else
                  {
                     functionName = matches.Groups[2].Value;
                     functionName = functionName[0].ToString().ToUpper() + functionName.Substring(1);
                     consoleFunctionList.Add(functionName);
                  }
               }
               if (!matches.Groups[4].Value.Equals(matches.Groups[5].Value))
                  overloadedFunctionList.Add(functionName);
            }
            if (line.StartsWith("DLL_PUBLIC"))
            {
               Match matches =
                  Regex.Matches(line,
                     "DLL_PUBLIC ([a-zA-Z\\* _0-9:<>]+) ([a-zA-Z\\* _0-9:]+)\\(\\s*(?:([a-zA-Z\\* _0-9:]+)[\\s,]*)*\\s*\\)")
                     [0];
               CFunction function = new CFunction(matches.Groups[1].Value, matches.Groups[2].Value,
                  matches.Groups[3].Captures);
               StringBuilder bodyBuilder = new StringBuilder();
               int indentation = 0;
               while ((line = SR.ReadLine()) != null)
               {
                  if (line.Contains("}"))
                     indentation--;
                  if (indentation != 0)
                     bodyBuilder.AppendLine(line);
                  if (line.Contains("{"))
                     indentation++;
                  if (indentation == 0)
                     break;
               }
               function.FunctionBody = bodyBuilder.ToString();
               functions.Add(function);
            }
         }
         SR.Close();
         return functions;
      }

      public void WriteToXML(string pT6scriptmetadata)
      {
         StreamWriter overloadedWriter = new StreamWriter("overloaded.log");
         foreach (string s in overloadedFunctionList)
         {
            overloadedWriter.WriteLine(s);
         }
         overloadedWriter.Close();
         List<string> notFoundFunctions =
            consoleMethodList.Where(x => !Functions.Any(f => f.FunctionName.ToLower().Equals(x.ToLower()))).ToList();
         foreach (string notFoundFunction in notFoundFunctions)
         {
            if (!IgnoredMethodList.Contains(notFoundFunction))
               log.AppendLine(String.Format("The method {0} might not have a corresponding CFunction",
                  notFoundFunction));
         }
         notFoundFunctions.Clear();
         foreach (string s in consoleFunctionList)
         {
            bool any = !Functions.Any(
               f => f.NameSpace != null
                    && f.FunctionName.ToLower().Equals(s.ToLower()));
            any =
               Functions.Any(
                  cFunction => cFunction.NameSpace != null && s.ToLower().EndsWith(cFunction.FunctionName.ToLower()));
            if (!any)
               notFoundFunctions.Add(s);
         }
         foreach (string notFoundFunction in notFoundFunctions)
         {
            if (!IgnoredFunctionList.Contains(notFoundFunction))
               log.AppendLine(String.Format("The function {0} might not have a corresponding CFunction",
                  notFoundFunction));
         }
         XmlWriterSettings settings = new XmlWriterSettings {Indent = true, IndentChars = "   "};
         XmlWriter writer = XmlWriter.Create(pT6scriptmetadata, settings);
         writer.WriteStartElement("Torque6MetaData");
         foreach (EngineClass t6Class in Classes)
         {
            if (t6Class.DerivesFromSimObject)
            {
               writer.WriteStartElement("Class");
               writer.WriteAttributeString("Name", t6Class.ClassName);
               writer.WriteAttributeString("Namespace", Namespaces.GetNameSpace(t6Class.ClassName));
               if (t6Class.ClassName != "SimObject")
               {
                  writer.WriteStartElement("Class.Parents");
                  foreach (string parentClassName in t6Class.ParentClassNames)
                  {
                     if (!t6Class.ParentClasses.ContainsKey(parentClassName) ||
                         !t6Class.ParentClasses[parentClassName].DerivesFromSimObject)
                        continue;
                     writer.WriteStartElement("Parent");
                     writer.WriteAttributeString("Name", parentClassName);
                     writer.WriteEndElement();
                  }
                  writer.WriteEndElement();
               }
               writer.WriteStartElement("Functions");
               foreach (CFunction cFunction in Functions)
               {
                  if (cFunction.OwnerClass == t6Class && !cFunction.PropertyFunction)
                  {
                     writer.WriteStartElement("Function");
                     writer.WriteAttributeString("Name", cFunction.FunctionName);
                     writer.WriteAttributeString("ReturnType", cFunction.FunctionVariableTypeInfo.ToString());
                     writer.WriteStartElement("Params");
                     foreach (CFunction.Param functionParam in cFunction.FunctionParams)
                     {
                        string defaultValue = DefaultValues.GetDefaultValue(cFunction.FunctionName, functionParam.Name);
                        writer.WriteStartElement("Param");
                        writer.WriteAttributeString("Name", functionParam.Name);
                        writer.WriteAttributeString("Type", functionParam.ParamTypeInfo.ToString());
                        writer.WriteAttributeString("DefaultValue", defaultValue ?? "N/A");
                        writer.WriteEndElement();
                     }
                     writer.WriteEndElement();
                     writer.WriteEndElement();
                  }
               }
               writer.WriteEndElement();
               writer.WriteStartElement("Properties");
               foreach (T6Field t6Field in t6Class.Fields)
               {
                  writer.WriteStartElement("Property");
                  writer.WriteAttributeString("Name", t6Field.FieldName);
                  writer.WriteAttributeString("Type", t6Field.FieldTypeInfo.ToString());
                  writer.WriteAttributeString("Count", t6Field.FieldCount.ToString());
                  if (t6Field.GetterFunction != null)
                  {
                     bool isOut = t6Field.GetterFunction.FunctionParams.Count == 3 && t6Field.FieldCount > 1
                                  || t6Field.GetterFunction.FunctionParams.Count == 2 && t6Field.FieldCount <= 1;
                     writer.WriteAttributeString("Out", isOut.ToString());
                     writer.WriteStartElement("Property.Getter");
                     writer.WriteAttributeString("Name", t6Field.GetterFunction.FunctionName);
                     writer.WriteAttributeString("ReturnType",
                        t6Field.GetterFunction.FunctionVariableTypeInfo.ToString());
                     writer.WriteStartElement("Getter.Params");
                     foreach (CFunction.Param functionParam in t6Field.GetterFunction.FunctionParams)
                     {
                        writer.WriteStartElement("Param");
                        writer.WriteAttributeString("Name", functionParam.Name);
                        writer.WriteAttributeString("Type", functionParam.ParamTypeInfo.ToString());
                        writer.WriteEndElement();
                     }
                     writer.WriteEndElement();
                     writer.WriteEndElement();
                  }
                  else
                  {
                     writer.WriteAttributeString("Out", "False");
                     log.AppendLine("No getter for field: " + t6Field.FieldName + " on class: " + t6Class.ClassName);
                  }
                  if (t6Field.SetterFunction != null)
                  {
                     writer.WriteStartElement("Property.Setter");
                     writer.WriteAttributeString("Name", t6Field.SetterFunction.FunctionName);
                     writer.WriteAttributeString("ReturnType",
                        t6Field.SetterFunction.FunctionVariableTypeInfo.ToString());
                     writer.WriteStartElement("Setter.Params");
                     foreach (CFunction.Param functionParam in t6Field.SetterFunction.FunctionParams)
                     {
                        writer.WriteStartElement("Param");
                        writer.WriteAttributeString("Name", functionParam.Name);
                        writer.WriteAttributeString("Type", functionParam.ParamTypeInfo.ToString());
                        writer.WriteEndElement();
                     }
                     writer.WriteEndElement();
                     writer.WriteEndElement();
                  }
                  else
                  {
                     log.AppendLine("No setter for field: " + t6Field.FieldName + " on class: " + t6Class.ClassName);
                  }
                  writer.WriteEndElement();
               }
               writer.WriteEndElement();
               writer.WriteEndElement();
            }
         }
         foreach (CFunction cFunction in Functions)
         {
            if (cFunction.NameSpace == null)
               continue;
            writer.WriteStartElement("Function");
            writer.WriteAttributeString("Name", cFunction.FunctionName);
            writer.WriteAttributeString("NameSpace", cFunction.NameSpace);
            writer.WriteAttributeString("ReturnType", cFunction.FunctionVariableTypeInfo.ToString());
            writer.WriteStartElement("Params");
            foreach (CFunction.Param functionParam in cFunction.FunctionParams)
            {
               writer.WriteStartElement("Param");
               writer.WriteAttributeString("Name", functionParam.Name);
               writer.WriteAttributeString("Type", functionParam.ParamTypeInfo.ToString());
               writer.WriteAttributeString("Out", functionParam.ParamTypeInfo.Out.ToString());
               writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
         }
         writer.WriteEndElement();
         writer.Close();
         StreamWriter SW = new StreamWriter("metadata.log");
         SW.Write(log.ToString());
         SW.Close();
      }

      public void AddBindingFile(string pFile)
      {
         List<CFunction> functions = ParseCFunctionsFromBindingFile(pFile);
         Functions = Functions.Concat(functions).ToList();
      }

      public void AddHeaderFile(string pFile)
      {
         List<EngineClass> classes = ParseClassesFromHeaderFile(pFile, Functions);
         Classes = Classes.Concat(classes).ToList();
      }

      public void AddCFile(string pFile)
      {
         List<T6Field> fields = ParseFieldsFromCCFile(pFile, Classes, Functions);
         Fields = Fields.Concat(fields).ToList();
      }

      public void GenerateHierarchy()
      {
         for (int index = Classes.Count - 1; index >= 0; index--)
         {
            EngineClass engineClass = Classes[index];
            foreach (string parentClassName in engineClass.ParentClassNames)
            {
               foreach (EngineClass innerT6Class in Classes)
               {
                  if (innerT6Class.ClassName.Equals(parentClassName))
                     engineClass.ParentClasses[parentClassName] = innerT6Class;
               }
            }
            if (engineClass.ParentClasses.Count <= 0 && engineClass.ClassName != "SimObject")
               Classes.Remove(engineClass);
         }
         foreach (EngineClass t6Class in Classes)
         {
            if (t6Class.DerivesFromSimObject)
               continue;
            t6Class.DerivesFromSimObject = t6Class.ParentsDerivesFromSimObject();
         }
      }

      public void SetFunctionNamespaces()
      {
         foreach (CFunction cFunction in Functions.Where(cFunction => cFunction.OwnerClass == null))
         {
            if (cFunction.FunctionName.Contains("_"))
            {
               cFunction.NameSpace = cFunction.FunctionName.Split('_')[0];
               cFunction.FunctionName = cFunction.FunctionName.Split('_')[1];
            }
            else
            {
               cFunction.NameSpace = "Engine";
            }
         }
      }
   }

   internal class DefaultValueCollection
   {
      public DefaultValueCollection(StringReader pStringReader)
      {
         Functions = new List<FunctionDefaultParam>();
         string line;
         FunctionDefaultParam function = null;
         while ((line = pStringReader.ReadLine()) != null)
         {
            line = line.Trim();
            if (line.StartsWith(":"))
            {
               function = new FunctionDefaultParam(line.Substring(1));
               Functions.Add(function);
            }
            if (line.StartsWith("-") && function != null)
            {
               string[] param = line.Substring(1).Split('=');
               function.DefaultParams.Add(param[0].Trim(), param[1].Trim());
            }
         }
      }

      public List<FunctionDefaultParam> Functions { get; set; }

      public string GetDefaultValue(string pFunctionName, string pName)
      {
         return (from functionDefaultParam in Functions
            where functionDefaultParam.FunctionName.Equals(pFunctionName)
                  && functionDefaultParam.DefaultParams.ContainsKey(pName)
                 select functionDefaultParam.DefaultParams[pName]).FirstOrDefault();
      }

      public class FunctionDefaultParam
      {
         public Dictionary<string, string> DefaultParams;
         public string FunctionName;

         public FunctionDefaultParam(string pFunctionName)
         {
            FunctionName = pFunctionName;
            DefaultParams = new Dictionary<string, string>();
         }
      }
   }

   internal class NamespaceCollection
   {
      public NamespaceCollection(StringReader pStreamReader, int pDepth, string pName)
      {
         ContainedSimObjects = new List<string>();
         ContainedNamespaces = new List<NamespaceCollection>();
         Name = pName;
         string line = pStreamReader.ReadLine();
         while (line != null)
         {
            // ReSharper disable once AssignNullToNotNullAttribute
            MatchCollection matchCollection = Regex.Matches(line, "(-*)(:)?(.+)");
            if (matchCollection.Count != 1)
               continue;
            Match match = matchCollection[0];
            int depth = match.Groups[1].Length;
            bool isNamespace = match.Groups[2].Success;
            string name = match.Groups[3].Value;
            if (isNamespace)
            {
               string content = "";
               while ((line = pStreamReader.ReadLine()) != null)
               {
                  // ReSharper disable once AssignNullToNotNullAttribute
                  matchCollection = Regex.Matches(line, "^(-*)(:)?([a-zA-Z0-9_\\*]+)$");
                  if (matchCollection.Count != 1)
                     continue;
                  match = matchCollection[0];
                  int nestedDepth = match.Groups[1].Length;
                  if (nestedDepth <= depth)
                     break;
                  content += line + Environment.NewLine;
               }
               ContainedNamespaces.Add(new NamespaceCollection(new StringReader(content), depth, name));
            }
            else
            {
               ContainedSimObjects.Add(name);
               line = pStreamReader.ReadLine();
            }
         }
         pStreamReader.Close();
      }

      public List<NamespaceCollection> ContainedNamespaces { get; set; }

      public List<string> ContainedSimObjects { get; set; }

      public string Name { get; set; }

      public string GetNameSpace(string pClassName)
      {
         if (ContainedSimObjects.Any(containedSimObject => containedSimObject.Equals(pClassName)
                                                           ||
                                                           (containedSimObject.EndsWith("*") &&
                                                            pClassName.StartsWith(containedSimObject.Substring(0,
                                                               containedSimObject.Length - 2)))))
         {
            return Name;
         }
         if (ContainedSimObjects.Contains(pClassName))
            return Name;
         return (from namespaceCollection in ContainedNamespaces
            where namespaceCollection.GetNameSpace(pClassName) != null
            select (Name != null ? Name + "." : "") + namespaceCollection.GetNameSpace(pClassName)).FirstOrDefault();
      }
   }
}