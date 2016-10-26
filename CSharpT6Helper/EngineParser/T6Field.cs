namespace CSharpT6Helper
{
   internal class T6Field
   {
      public string FieldName { get; set; }
      public TypeData FieldTypeInfo { get; set; }
      public int FieldCount { get; set; }
      public CFunction GetterFunction { get; set; }
      public CFunction SetterFunction { get; set; }

      public static TypeData TypeFromString(string pType)
      {
         if (pType.StartsWith("Graphics::"))
            pType = pType.Substring("Graphics::".Length);
         string controlString = pType.ToLower().Trim();
         switch (controlString)
         {
            case "const char**":
            case "char**":
               return TypeData.StringArray;
            case "int*":
            case "s32*":
               return TypeData.IntArray;
            case "const char*":
            case "char*":
            case "stringtableentry":
            case "typefilename":
            case "typesimobjectname":
            case "typeassetloosefilepath":
            case "typestring":
            case "typecasestring":
            case "typeassetid":
               return TypeData.String;
            case "bool":
            case "const bool":
            case "typebool":
               return TypeData.Bool;
            case "int":
            case "typeenum":
            case "s32":
            case "const s32":
            case "types32":
            case "filestream::accessmode":
               return TypeData.Int;
            case "u32":
            case "const u32":
               return TypeData.UInt;
            case "u8":
            case "const u8":
               return TypeData.UChar;
            case "u16":
            case "const u16":
               return TypeData.UShort;
            case "float":
            case "f32":
            case "const f32":
            case "typef32":
               return TypeData.Float;
            case "void":
               return TypeData.Void;
            case "typecolori":
            case "typecolorf":
            case "cinterface::colorparam":
               return TypeData.Color;
            case "cinterface::colorparam*":
               return new TypeData("Color", true);
            case "point2i":
            case "typepoint2i":
            case "cinterface::point2iparam":
               return TypeData.Point2I;
            case "cinterface::point2iparam*":
               return new TypeData("Point2I", true);
            case "point2f":
            case "typepoint2f":
            case "cinterface::point2fparam":
               return TypeData.Point2F;
            case "cinterface::point2fparam*":
               return new TypeData("Point2F", true);
            case "point3i":
            case "typepoint3i":
            case "cinterface::point3iparam":
               return TypeData.Point3I;
            case "cinterface::point3iparam*":
               return new TypeData("Point3I", true);
            case "point3f":
            case "typepoint3f":
            case "cinterface::point3fparam":
               return TypeData.Point3F;
            case "cinterface::point3fparam*":
               return new TypeData("Point3F", true);
            case "box3f":
            case "typebox3f":
            case "cinterface::box3fparam":
               return TypeData.Box3F;
            case "cinterface::box3fparam*":
               return new TypeData("Box3F", true);
            case "recti":
            case "typerecti":
            case "cinterface::rectiparam":
               return TypeData.RectI;
            case "cinterface::rectiparam*":
               return new TypeData("RectI", true);
            case "point4f":
            case "typepoint4f":
            case "cinterface::point4fparam":
               return TypeData.Point4F;
            case "cinterface::point4fparam*":
               return new TypeData("Point4F", true);
            case "typeaudioassetptr":
               return new TypeData("SimObjectPtr")
               {
                  DataName = "AudioAsset"
               };
            case "typesimobjectptr":
               return new TypeData("SimObjectPtr")
               {
                  DataName = "SimObject" //TODO make this more specific
               };
            case "typeguiprofile":
               return new TypeData("SimObjectPtr")
               {
                  DataName = "GuiControlProfile"
               };
            default:
               if (controlString.EndsWith("**"))
                  return new TypeData("SimObjectPtrArray")
                  {
                     DataName = pType.Trim().Substring(0, pType.Trim().Length - 1)
                  };
               if (controlString.EndsWith("*"))
                  return new TypeData("SimObjectPtr")
                  {
                     DataName = pType.Trim().Substring(0, pType.Trim().Length - 1)
                  };
               return TypeData.Unknown;
         }
      }

      internal class TypeData
      {
         public static readonly TypeData Unknown = new TypeData("Unknown");
         public static readonly TypeData String = new TypeData("String");
         public static readonly TypeData StringArray = new TypeData("StringArray");
         public static readonly TypeData IntArray = new TypeData("IntArray");
         public static readonly TypeData Int = new TypeData("Int");
         public static readonly TypeData UInt = new TypeData("UInt");
         public static readonly TypeData UChar = new TypeData("UChar");
         public static readonly TypeData UShort = new TypeData("UShort");
         public static readonly TypeData SimObjectPtr = new TypeData("SimObjectPtr");
         public static readonly TypeData Float = new TypeData("Float");
         public static readonly TypeData Bool = new TypeData("Bool");
         public static readonly TypeData Void = new TypeData("Void");
         public static readonly TypeData Color = new TypeData("Color");
         public static readonly TypeData Point2I = new TypeData("Point2I");
         public static readonly TypeData Point2F = new TypeData("Point2F");
         public static readonly TypeData Point3I = new TypeData("Point3I");
         public static readonly TypeData Point3F = new TypeData("Point3F");
         public static readonly TypeData Point4F = new TypeData("Point4F");
         public static readonly TypeData Box3F = new TypeData("Box3F");
         public static readonly TypeData RectI = new TypeData("RectI");
         private readonly string dataType;

         public TypeData(string pType)
         {
            dataType = pType;
            Out = false;
         }

         public TypeData(string pType, bool pOut)
         {
            dataType = pType;
            Out = pOut;
         }

         public string DataName { get; set; }
         public bool Out { get; set; }

         public override string ToString()
         {
            return dataType.Equals("SimObjectPtr") ? DataName : dataType;
         }

         protected bool Equals(TypeData other)
         {
            return string.Equals(dataType, other.dataType);
         }

         public override bool Equals(object obj)
         {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeData) obj);
         }

         public override int GetHashCode()
         {
            unchecked
            {
               return ((dataType != null ? dataType.GetHashCode() : 0)*397) ^
                      (DataName != null ? DataName.GetHashCode() : 0);
            }
         }

         public static bool operator ==(TypeData td1, TypeData td2)
         {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(td1, td2))
            {
               return true;
            }

            // If one is null, but not both, return false.
            if (((object)td1 == null) || ((object)td2 == null))
            {
               return false;
            }
            return td1.dataType.Equals(td2.dataType);
         }

         public static bool operator !=(TypeData td1, TypeData td2)
         {
            return !(td1 == td2);
         }
      }
   }
}