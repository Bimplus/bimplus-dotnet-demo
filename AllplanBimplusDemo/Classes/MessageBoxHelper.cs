using System.Windows;

namespace AllplanBimplusDemo.Classes
{
    public static class MessageBoxHelper
    {
        public static void ShowInformation(string message, Window window = null)
        {
            if (window == null)
                MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(window, message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool ShowQuestion(string message, MessageBoxResult defaultResult = MessageBoxResult.OK, Window window = null)
        {
            MessageBoxResult result = MessageBoxResult.Yes;
            if (window == null)
                result = MessageBox.Show(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, defaultResult);
            else
                result = MessageBox.Show(window, message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, defaultResult);
            return result == MessageBoxResult.Yes;
        }
    }
}
