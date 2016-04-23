using System.Windows;

namespace CSharpT6Helper
{
   /// <summary>
   /// Interaction logic for DisplayGenerated.xaml
   /// </summary>
   public partial class DisplayGenerated : Window
   {
      public DisplayGenerated(string pGenerateCInterface)
      {
         InitializeComponent();
         ContentBlock.Text = pGenerateCInterface;
      }
   }
}
