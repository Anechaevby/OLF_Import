using System.IO;
using System.Windows;
using OLF_Import.Model;

namespace OLF_Import.Forms
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            var model = GetMainModel();
            if (model != null)
            {
                var pathAbout = Path.Combine(Directory.GetCurrentDirectory(), "AboutContent.txt");
                if (File.Exists(pathAbout))
                {
                    var content = File.ReadAllText(pathAbout);
                    model.RepresentationDoc = content;
                }
            }
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private MainFormViewModel GetMainModel()
        {
            var model = FindResource("mainModel");
            var mainFormViewModel = model as MainFormViewModel;
            return mainFormViewModel;
        }

    }
}
