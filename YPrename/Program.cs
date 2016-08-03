using Svetomech.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace YPrename
{
  internal static class Program
  {
    private const string renameFilesOldPattern = "YP-*1R-*";
    private const string renameFilesNewPattern = "*";

    static void Main(string[] args)
    {
      Console.Title = Application.ProductName;
      Console.WriteLine();

      string currentFolderPath = Directory.GetCurrentDirectory();

      var filesToRename = new List<string>(Directory.GetFiles(currentFolderPath, renameFilesOldPattern));

      bool searchAccepted = confirmSearchDialog();

      if (!searchAccepted)
      {
        messageLine("Exit.");
        return;
      }

      Console.WriteLine();
      renameFiles(filesToRename);

      using (var watcher = new FileSystemWatcher())
      {
        watcher.Path = currentFolderPath;
        watcher.NotifyFilter = NotifyFilters.FileName;

        watcher.Created += new FileSystemEventHandler(renameFileCreated);
        watcher.Renamed += new RenamedEventHandler(renameFileMoved);

        watcher.EnableRaisingEvents = true;

        string searchPauseText = "stop";
        Console.Title = $"{Application.ProductName}: Press 'p' to {searchPauseText} searching for new files.";

        for (;;)
        {
          char pressedKey = char.ToLower(Console.ReadKey(true).KeyChar);

          if ('p' != pressedKey)
          {
            continue;
          }

          watcher.EnableRaisingEvents = !watcher.EnableRaisingEvents;

          searchPauseText = (searchPauseText == "stop") ? "continue" : "stop";
          Console.Title = $"{Application.ProductName}: Press 'p' to {searchPauseText} searching for new files.";
        }
      }
    }

    private static bool confirmSearchDialog()
    {
      Console.Write("Start searching for files to rename? [Y/n/?] ");

      return (char.ToLower(Console.ReadKey(true).KeyChar) == 'y');
    }

    private static void messageLine(string text)
    {
      SimpleConsole.Line.ClearCurrent();
      Console.WriteLine(text);
      Console.ReadKey(true);
    }

    private static void renameFiles(List<string> filePaths)
    {
      if (0 == filePaths.Count)
      {
        return;
      }

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

      if (!File.Exists(filePathRenamed) && !SimpleIO.Path.Equals(filePath, filePathRenamed))
      {
        File.Move(filePath, filePathRenamed);
        Console.WriteLine($"\n{Path.GetFileName(filePath)} -> {fileName}");
      }
      else
      {
        Console.WriteLine($"\n[X] {Path.GetFileName(filePath)} -> {fileName}");
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
