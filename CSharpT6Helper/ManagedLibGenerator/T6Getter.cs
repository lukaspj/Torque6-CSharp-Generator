using System.Collections.Generic;

namespace CSharpT6Helper
{
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
         Parameters = new List<T6Parameter>();
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
         return GetterType.NativeArgType + " " + GetterName + indexString;
      }

      public virtual T6Function ToT6Function()
      {
         return new T6Function(PropGetterName, null, PropReturnType)
         {
            Parameters = Parameters
         };
      }
   }
}