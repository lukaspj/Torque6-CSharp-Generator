using System.Collections.Generic;
using CSharpT6Helper;

internal class EngineClass
{
   public bool DerivesFromSimObject;

   public EngineClass(string pName)
   {
      ClassName = pName;
      ParentClassNames = new List<string>();
      ParentClasses = new Dictionary<string, EngineClass>();
      Fields = new List<T6Field>();
      DerivesFromSimObject = ClassName == "SimObject";
   }

   public string ClassName { get; set; }
   public List<string> ParentClassNames { get; set; }
   public Dictionary<string, EngineClass> ParentClasses { get; set; }
   public List<T6Field> Fields { get; set; }

   public bool ParentsDerivesFromSimObject()
   {
      foreach (KeyValuePair<string, EngineClass> parentClass in ParentClasses)
      {
         if (parentClass.Key == "SimObject"
             || parentClass.Value.DerivesFromSimObject)
            return true;
         parentClass.Value.DerivesFromSimObject = parentClass.Value.ParentsDerivesFromSimObject();
         if (parentClass.Value.DerivesFromSimObject)
            return true;
      }
      return false;
   }
}