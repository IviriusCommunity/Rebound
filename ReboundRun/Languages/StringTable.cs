using System.Globalization;

namespace ReboundRun.Languages
{
    public class StringTable
    {
        public static string AppTitle;
        public static string Run;
        public static string RunAsAdmin;
        public static string Description;
        public static string Open;
        public static string Arguments;
        public static string Cancel;
        public static string Browse;

        public StringTable()
        {
            ReadLanguage();
        }

        public static void ReadLanguage()
        {
            // Get the current culture (language) of the system
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            if (currentCulture.Name.ToLower() == "en-us")
            {
                AppTitle = "Rebound Run";
                Run = "Run";
                RunAsAdmin = "Run as Administrator";
                Description = "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.";
                Open = "Open";
                Arguments = "Arguments";
                Cancel = "Cancel";
                Browse = "Browse";
            }
            if (currentCulture.Name.ToLower() == "ro-ro")
            {
                AppTitle = "Executare Rebound";
                Run = "Execută";
            }
            if (currentCulture.Name.ToLower() == "de-de")
            {
                AppTitle = "Rebound Ausführen";
                Run = "Ausführen";
                RunAsAdmin = "Als Administrator ausführen";
                Description = "Geben Sie den Namen eines Programms, Ordners, Dokuments oder einer Internetressource an.";
                Open = "Öffnen";
                Arguments = "Argumente";
                Cancel = "Abbrechen";
                Browse = "Druchsuchen";
            }
        }
    }
}
