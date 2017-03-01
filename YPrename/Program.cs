using Svetomech.Utilities;
using Svetomech.Utilities.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static Svetomech.Utilities.ConsoleApplication;
using static Svetomech.Utilities.NativeMethods;
using static Svetomech.Utilities.SimpleConsole;

namespace YPrename
{
    internal static class Program
    {
        private const string renameFilesOldPattern = "YP-*??-*";
        private const string renameFilesNewPattern = "*";

        private static readonly Window mainWindow = GetConsoleWindow();
        // private static AutoResetEvent autoEvent;

        static void Main(string[] args)
        {
            Console.Title = ProductName;

            string currentFolderPath = ((args.Length > 0) && Directory.Exists(args[0])) ? args[0] : StartupPath;

            var filesToRename = new List<string>(Directory.GetFiles(currentFolderPath, renameFilesOldPattern));

            renameFiles(filesToRename);

            using (var watcher = new FileSystemWatcher())
            {
                watcher.Path = currentFolderPath;
                watcher.NotifyFilter = NotifyFilters.FileName;

                watcher.Created += new FileSystemEventHandler(renameFileCreated);
                watcher.Renamed += new RenamedEventHandler(renameFileMoved);

                watcher.EnableRaisingEvents = true;

                Thread.Sleep(Timeout.Infinite); // autoEvent.WaitOne();
            }
        }

        private static bool confirmRenameDialog()
        {
            Console.Write(" [Y/n/?] ");

            return (char.ToLower(Console.ReadKey().KeyChar) == 'y');
        }

        private static void renameFiles(List<string> filePaths)
        {
            if (0 == filePaths.Count)
            {
                Console.Title = ProductName;
                mainWindow.Hide();
                return;
            }

            Console.Title = $"{ProductName}: NEW FILE FOUND";
            mainWindow.Show();

            string filePath = filePaths[filePaths.Count - 1];
            string fileDirectory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            string[] partsToReplace = renameFilesOldPattern.Split('*');
            string[] partsToReplaceWith = renameFilesNewPattern.Split('*');

            for (int i = 0; i < partsToReplaceWith.Length; ++i)
            {
                if (partsToReplace[i].Contains("?"))
                {
                    // Helper variables, only needed to clarify meaning
                    char lastSignificantCharacter = renameFilesOldPattern[renameFilesOldPattern.LastIndexOf('*') - 1];
                    int questionMarkCount = renameFilesOldPattern.Length - renameFilesOldPattern.Replace("?", "").Length;

                    // Actual variables
                    int baseDisposition = 0; // ... to keep track of removed question marks
                    int baseIndexOfQM = fileName.LastIndexOf(lastSignificantCharacter) - questionMarkCount; // ... in fileName
                    int indexOfQM = -1; // ... in renameFilesOldPattern

                    // Replaces all question marks with actual characters that represent them
                    while (-1 != (indexOfQM = partsToReplace[i].IndexOf("?")))
                    {
                        partsToReplace[i] = partsToReplace[i].Remove(indexOfQM, 1);
                        partsToReplace[i] = partsToReplace[i].Insert(indexOfQM, fileName[baseIndexOfQM + baseDisposition++].ToString());
                    }
                }

                fileName = fileName.Replace(partsToReplace[i], partsToReplaceWith[i]);
            }
            string filePathRenamed = Path.Combine(fileDirectory, fileName);

            string logMessage = $"[ ] \"{Path.GetFileName(filePath)}\" -> \"{fileName}\"";
            Console.WriteLine();
            Console.Write(logMessage);
            bool renamingAccepted = confirmRenameDialog();
            Line.ClearCurrent();

            if (!renamingAccepted)
            {
                logMessage = logMessage.Replace("[ ]", "[n]");
            }
            else if (File.Exists(filePathRenamed) || SimpleIO.Path.Equals(filePath, filePathRenamed))
            {
                logMessage = logMessage.Replace("[ ]", "[!]");
            }
            else
            {
                logMessage = logMessage.Replace("[ ]", "[y]");
                try { File.Move(filePath, filePathRenamed); }
                catch { logMessage = logMessage.Replace("[ ]", "[!]"); }
            }

            Console.WriteLine(logMessage);

            filePaths.Remove(filePath);
            renameFiles(filePaths);
        }

        private static void renameFileCreated(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);

            if (!file.FitsMask(renameFilesOldPattern))
            {
                return;
            }

            while (file.IsLocked())
            {
                Thread.Sleep(1);
            }

            renameFiles(new List<string> { e.FullPath });
        }

        private static void renameFileMoved(object sender, FileSystemEventArgs e)
        {
            renameFileCreated(sender, e);
        }
    }
}
