namespace CSharpT6Helper
{
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
}