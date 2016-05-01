using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CSharpT6Helper
{
   /// <summary>
   ///    Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void GenerateCInterfaceButton_OnClick(object pSender, RoutedEventArgs pE)
      {
         DisplayGenerated display = new DisplayGenerated(Generator.GenerateCInterface(GenerateCInterfaceTextBox.Text));
         display.Show();
      }

      private void GenerateCSharpInterfaceButton_OnClick(object pSender, RoutedEventArgs pE)
      {
         DisplayGenerated display = new DisplayGenerated(Generator.GenerateCSharpInterface(CSharpTextBox.Text));
         display.Show();
      }

      private async void GenerateFromFolderButton_OnClick(object pSender, RoutedEventArgs pE)
      {
         CommonOpenFileDialog dialog = new CommonOpenFileDialog {IsFolderPicker = true};
         dialog.ShowDialog();
         
         string[] bindingFiles = Directory.GetFiles(dialog.FileName, "*_Binding*.*", SearchOption.AllDirectories);
         string[] headerFiles = Directory.GetFiles(dialog.FileName, "*.h", SearchOption.AllDirectories).Except(bindingFiles).ToArray();
         string[] cFiles = Directory.GetFiles(dialog.FileName, "*.c*", SearchOption.AllDirectories).Except(bindingFiles).ToArray();
         TSClassMetaData GeneratedClassMetaData = new TSClassMetaData
         {
            Classes = new List<EngineClass>(),
            Functions = new List<CFunction>(),
            Fields = new List<T6Field>()
         };
         GenerateProgressBar.Maximum = bindingFiles.Count();
         GenerateProgressBar.Value = 0;
         foreach (string file in bindingFiles)
         {
            string _file = file;
            await Task.Run(() => GeneratedClassMetaData.AddBindingFile(_file));
            GenerateProgressBar.Value++;
         }
         GenerateProgressBar.Maximum = headerFiles.Count();
         GenerateProgressBar.Value = 0;
         foreach (string file in headerFiles)
         {
            string _file = file;
            await Task.Run(() => GeneratedClassMetaData.AddHeaderFile(_file));
            GenerateProgressBar.Value++;
         }
         GenerateProgressBar.Maximum = cFiles.Count();
         GenerateProgressBar.Value = 0;
         foreach (string file in cFiles)
         {
            string _file = file;
            await Task.Run(() => GeneratedClassMetaData.AddCFile(_file));
            GenerateProgressBar.Value++;
         }
         
         GeneratedClassMetaData.GenerateHierarchy();
         GeneratedClassMetaData.SetFunctionNamespaces();

         GeneratedClassMetaData.WriteToXML("T6ScriptMetaData.xml");
      }

      private void ParseGeneratedbutton_OnClick(object pSender, RoutedEventArgs pE)
      {
         CSharpGenerator.GenerateFromXmlFile("T6ScriptMetaData.xml");
      }

      private async void MalpBindingButton_Click(object sender, RoutedEventArgs e)
      {
         CommonOpenFileDialog dialog = new CommonOpenFileDialog {IsFolderPicker = true};
         dialog.ShowDialog();

         string[] bindingFiles = Directory.GetFiles(dialog.FileName, "*_Binding*.*", SearchOption.AllDirectories);
         string[] cFiles = Directory.GetFiles(dialog.FileName, "*.c*", SearchOption.AllDirectories).Except(bindingFiles).ToArray();
         TSClassMetaData GeneratedClassMetaData = new TSClassMetaData
         {
            Classes = new List<EngineClass>(),
            Functions = new List<CFunction>(),
            Fields = new List<T6Field>()
         };
         GenerateProgressBar.Maximum = cFiles.Count();
         GenerateProgressBar.Value = 0;
         foreach (string file in cFiles)
         {
            string _file = file;
            await Task.Run(() => GeneratedClassMetaData.AddBindingFile(_file));
            GenerateProgressBar.Value++;
         }

         GeneratedClassMetaData.GenerateHierarchy();
         GeneratedClassMetaData.SetFunctionNamespaces();

         GeneratedClassMetaData.WriteToXML("bs.xml");
      }
   }
}