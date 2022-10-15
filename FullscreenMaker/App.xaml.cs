using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace FullscreenMaker {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            WriteExceptionLog(e.Exception);
            MessageBox.Show("An exception was thrown", "Exception thrown", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void WriteExceptionLog(Exception e) {
            var currentDate = DateTime.Now;
            var executingPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            using (var writer = new StreamWriter($"{executingPath}\\crashlog_{currentDate:dd-MM-yyyy_HH-mm-ss}.txt", false)) {
                writer.Write(e.ToString());
            }
        }
    }
}
