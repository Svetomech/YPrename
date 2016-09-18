using Svetomech.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static Svetomech.Utilities.SimpleConsole;

namespace YPrename
{
  internal static class Program
  {
    private const string renameFilesOldPattern = "YP-*1R-*";
    private const string renameFilesNewPattern = "*";

    private static IntPtr mainWindowHandle;
    // private static AutoResetEvent autoEvent;

    static void Main(string[] args)
    {
      Console.Title = Application.ProductName;

      string currentFolderPath = Application.StartupPath;

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
        Console.Title = Application.ProductName;
        return;
      }

      Console.Title = $"{Application.ProductName}: NEW FILE FOUND";

      string filePath = filePaths[filePaths.Count - 1];
      string fileDirectory = Path.GetDirectoryName(filePath);
      string fileName = Path.GetFileName(filePath);

      string[] partsToReplace = renameFilesOldPattern.Split('*');
      string[] partsToReplaceWith = renameFilesNewPattern.Split('*');

      for (int i = 0; i < partsToReplaceWith.Length; ++i)
      {
        fileName = fileName.Replace(partsToReplace[i], partsToReplaceWith[i]);
      }
      string filePathRenamed = Path.Combine(fileDirectory, fileName);

      Console.Write($"\n[ ] \"{Path.GetFileName(filePath)}\" -> \"{fileName}\"");
      bool renamingAccepted = confirmRenameDialog();
      Line.ClearCurrent();

      if (!renamingAccepted)
      {
        Console.WriteLine($"[n] \"{Path.GetFileName(filePath)}\" -> \"{fileName}\"");
      }
      else if (File.Exists(filePathRenamed) || SimpleIO.Path.Equals(filePath, filePathRenamed))
      {
        Console.WriteLine($"[!] \"{Path.GetFileName(filePath)}\" -> \"{fileName}\"");
      }
      else
      {
        File.Move(filePath, filePathRenamed);
        Console.WriteLine($"[y] \"{Path.GetFileName(filePath)}\" -> \"{fileName}\"");
      }

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
