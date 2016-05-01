using System.Collections.Generic;

namespace CSharpT6Helper
{
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
         Parameters = new List<T6Parameter>();
      }

      public string PropSetterName { get; set; }
      public CSharpGenerator.CSharpType PropReturnType { get; set; }
      public string SetterName { get; set; }
      public CSharpGenerator.CSharpType SetterType { get; set; }
      public string SetterName2 { get; set; }
      public CSharpGenerator.CSharpType SetterType2 { get; set; }
      public List<T6Parameter> Parameters { get; set; }
      public bool HasIndex { get; set; }

      public virtual string GetParamString()
      {
         string indexString = "";
         if (HasIndex)
            indexString = ", int index";
         return SetterType.NativeArgType + " " + SetterName + indexString
                + ", " + SetterType2.NativeArgType + " " + SetterName2;
      }

      public T6Function ToT6Function()
      {
         return new T6Function(PropSetterName, null, PropReturnType)
         {
            Parameters = Parameters
         };
      }
   }
}