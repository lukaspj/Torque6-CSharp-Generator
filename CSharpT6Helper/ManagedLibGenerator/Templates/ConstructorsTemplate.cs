namespace CSharpT6Helper.ManagedLibGenerator.Templates
{
   class ConstructorsTemplate
   {
      public static string GetConstructorsString(string name)
      {
         if (name.Equals("SimObject"))
         {
            return
               $@"public {name}()
      {{
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{name}CreateInstance());
         ManagedSimTable.RegisterObject(this);
      }}

      public {name}(bool create = true)
      {{
         if(!create) return;
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{name}CreateInstance());
         ManagedSimTable.RegisterObject(this);
      }}

      public {name}(uint pId)
      {{
         ObjectPtr = Sim.FindObjectWrapperById(pId);
      }}

      public {name}(string pName)
      {{
         ObjectPtr = Sim.FindObjectWrapperByName(pName);
      }}

      public {name}(IntPtr pObjPtr)
      {{
         ObjectPtr = Sim.WrapObject(pObjPtr);
      }}

      public {name}(Sim.SimObjectPtr* pObjPtr)
      {{
         ObjectPtr = pObjPtr;
      }}

      public {name}(SimObject pObj)
      {{
         ObjectPtr = pObj.ObjectPtr;
      }}";
         }

         return
            $@"public {name}()
         : base(false)
      {{
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{name}CreateInstance());
         ManagedSimTable.RegisterObject(this);
      }}

      public {name}(bool create)
         : base(false)
      {{
         if(!create) return;
         ObjectPtr = Sim.WrapObject(InternalUnsafeMethods.{name}CreateInstance());
         ManagedSimTable.RegisterObject(this);
      }}

      public {name}(uint pId) : base(pId)
      {{
      }}

      public {name}(string pName) : base(pName)
      {{
      }}

      public {name}(IntPtr pObjPtr) : base(pObjPtr)
      {{
      }}

      public {name}(Sim.SimObjectPtr* pObjPtr) : base(pObjPtr)
      {{
      }}

      public {name}(SimObject pObj) : base(pObj)
      {{
      }}";
      }
   }
}
