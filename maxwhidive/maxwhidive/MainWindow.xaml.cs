using System;
using System.Windows;
using System.Windows.Controls;
using Maxwhidive;
using Microsoft.Win32;

namespace maxwhidive
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        public string inputPath { get; private set; }
        public string outputPath { get; private set; }
        public string cssline { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            cssline = "";
            inputPath = "";
            outputPath = "";
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Button_arqvtt(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Selecione o VTT:";
            openFileDialog.Filter = "Text files (*.vtt)|*.vtt";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                inputPath = openFileDialog.FileName;

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Salvar o ASS em:";
                saveFileDialog.Filter = "Text files (*.ass)|*.ass";
                if (saveFileDialog.ShowDialog() == true) {
                    outputPath = saveFileDialog.FileName;
                    vttname.Content = openFileDialog.SafeFileName;
                    mahouka.Content = "Saida como: "+saveFileDialog.SafeFileName;
                }
            }
        }

        private void Button_arqcss(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.css)|*.css";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                cssline = openFileDialog.FileName;
                cssname.Content = openFileDialog.SafeFileName;
            }
        }

        private void Button_OK(object sender, RoutedEventArgs e)
        {
            if (inputPath == null || inputPath == "" || outputPath == null || outputPath == "") { MessageBox.Show("Selecione um VTT!"); }
            else {
                SubtitleConverter subConv = new SubtitleConverter();
                subConv.ConvertSubtitle(inputPath, outputPath, cssline);
                mahouka.Content = "OK!  :v";
            }
        }

        private void Button_OKlogin(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Em Breve...");
        }
    }
}
