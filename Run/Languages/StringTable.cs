using System.Globalization;

namespace Rebound.Run.Languages;

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
    public static string Hover;
    public static string RunAsAdminLegacy;
    public static string RunLegacy;
    public static string RunAsAdminLegacyTooltip;
    public static string RunAsAdminTooltip;
    public static string RunLegacyTooltip;
    public static string RunTooltip;
    public static string SelectFileToRun;
    public static string ErrorMessage;
    public static string Error;
    public static string Warning;
    public static string WarningMessage;
    public static string ErrorMessage2;
    public static string Win3UIBoxAlreadyOpened;

    public StringTable()
    {
        ReadLanguage();
    }

    public static void ReadLanguage()
    {
        // Get the current culture (language) of the system
        var currentCulture = CultureInfo.CurrentUICulture;
        switch (currentCulture.Name.ToLower())
        {
            case "en-us":
                AppTitle = "Rebound Run";
                Run = "Run";
                RunAsAdmin = "Run as Administrator";
                Description = "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.";
                Open = "Open";
                Arguments = "Arguments";
                Cancel = "Cancel";
                Browse = "Browse";
                Hover = "Hover for information";
                RunAsAdminLegacy = "Run as Administrator (legacy)";
                RunLegacy = "Run legacy";
                RunAsAdminLegacyTooltip = "Rebound 11 replaces run entries of classic Windows applets with Rebound apps. To launch the legacy applets as administrator, use this option instead. (Also launches Task Manager without WinUI 3.)";
                RunAsAdminTooltip = "Run the selected process as administrator. This option will attempt to launch the corresponding Rebound 11 counterpart of the chosen task.";
                RunLegacyTooltip = "Rebound 11 replaces run entries of classic Windows applets with Rebound apps. To launch the legacy applets, use this option instead. (Also launches Task Manager without WinUI 3.)";
                RunTooltip = "This option will attempt to launch the corresponding Rebound 11 counterpart of the chosen task.";
                SelectFileToRun = "Select file to run";
                ErrorMessage = "The system cannot find the file specified or the command line arguments are invalid.";
                Error = "Error";
                Warning = "Important";
                WarningMessage = "You will have to open this app again to bring back the Windows + R invoke command for Rebound Run.";
                ErrorMessage2 = "The system cannot find the file specified.";
                Win3UIBoxAlreadyOpened = "The WinUI 3 run box is already opened.";
                break;
            case "ro-ro":
                AppTitle = "Executare Rebound";
                Run = "Execută";
                break;
            case "de-de":
                AppTitle = "Rebound Ausführen";
                Run = "Ausführen";
                RunAsAdmin = "Als Administrator ausführen";
                Description = "Geben Sie den Namen eines Programms, Ordners, Dokuments oder einer Internetressource an.";
                Open = "Öffnen";
                Arguments = "Argumente";
                Cancel = "Abbrechen";
                Browse = "Druchsuchen";
                break;
            case "ru-ru":
                AppTitle = "Rebound Выполнить";
                Run = "Запустить";
                RunAsAdmin = "Запуск от Администрартора";
                Description = "Ведите имя программы, папки, документа или ресурса Интернета, которые требуется открыть.";
                Open = "Открыть";
                Arguments = "Аргументы";
                Cancel = "Отмена";
                Browse = "Обзор...";
                Hover = "Наведите курсор на элемент для информации";
                RunAsAdminLegacy = "Запуск от Администрартора (устаревший)";
                RunLegacy = "Запустить устаревшую версию";
                RunAsAdminLegacyTooltip = "Rebound 11 заменяет классические приложения Windows на Rebound. Чтобы запустить устаревшие приложения от имени администратора, используйте эту опцию. (Также запускает диспетчер задач из Windows 10).";
                RunAsAdminTooltip = "Запускает приложение от имени Администратора. При использовании этой опции будут использоваться приложения от Rebound.";
                RunLegacyTooltip = "Rebound 11 заменяет классические приложения Windows на Rebound. Чтобы запустить устаревшие приложения, используйте эту опцию. (Также запускает диспетчер задач из Windows 10).";
                RunTooltip = "При использовании этой опции будут использоваться приложения от Rebound.";
                SelectFileToRun = "Выберите файл для запуска";
                ErrorMessage = "Не удаётся найти указанный файл. Проверьте, правильно ли указано имя и повторите попытку.";
                Error = "Ошибка";
                Warning = "Внимание";
                WarningMessage = "Вам нужно будет снова открыть это приложение, чтобы вернуть команду Windows + R Выполнить для Rebound Выполнить.";
                ErrorMessage2 = "Не удаётся найти указанный файл.";
                Win3UIBoxAlreadyOpened = "Окно выполнить WinUI 3 уже открыто.";
                break;
            default:
                AppTitle = "Rebound Run";
                Run = "Run";
                RunAsAdmin = "Run as Administrator";
                Description = "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.";
                Open = "Open";
                Arguments = "Arguments";
                Cancel = "Cancel";
                Browse = "Browse";
                Hover = "Hover for information";
                RunAsAdminLegacy = "Run as Administrator (legacy)";
                RunLegacy = "Run legacy";
                RunAsAdminLegacyTooltip = "Rebound 11 replaces run entries of classic Windows applets with Rebound apps. To launch the legacy applets as administrator, use this option instead. (Also launches Task Manager without WinUI 3.)";
                RunAsAdminTooltip = "Run the selected process as administrator. This option will attempt to launch the corresponding Rebound 11 counterpart of the chosen task.";
                RunLegacyTooltip = "Rebound 11 replaces run entries of classic Windows applets with Rebound apps. To launch the legacy applets, use this option instead. (Also launches Task Manager without WinUI 3.)";
                RunTooltip = "This option will attempt to launch the corresponding Rebound 11 counterpart of the chosen task.";
                SelectFileToRun = "Select file to run";
                ErrorMessage = "The system cannot find the file specified or the command line arguments are invalid.";
                Error = "Error";
                Warning = "Important";
                WarningMessage = "You will have to open this app again to bring back the Windows + R invoke command for Rebound Run.";
                ErrorMessage2 = "The system cannot find the file specified.";
                Win3UIBoxAlreadyOpened = "The WinUI 3 run box is already opened.";
                break;
        }
    }
}
