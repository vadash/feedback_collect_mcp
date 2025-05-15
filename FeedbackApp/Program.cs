using System.Windows;

namespace FeedbackApp
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var application = new Application();
            application.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            application.Run();
        }
    }
} 