# About the Project

A small and fast tool to find a search term in text files. Created with Rider/Visual Studio and .NET 8

![main.png](images/mainwindow.png)

To avoid for example to open big files in the preview window or search just for a limited range of files I added some settings:\

![settings.png](images/settings.png)

### Extensions

Type Extensions like txt, xm*, etc.\
I don't use extension like *.txt, just txt because I don't see any use of it. If there is an use to search for files like *test.txt or something like that I will think about it. But I never experienced it

This is how the PlugIn is reading the files:\
`Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.AllDirectories)`

## Download

Just download the zip file and extract it somewhere. There is no setup file.

Download (latest version): [FindInFiles-2024.11.20.13.zip](https://github.com/lukas-adrian/FindInFiles/blob/master/FindInFiles-2024.11.20.13.zip)

## AvalonEdit as PreviewWindow

I am using AvalonEdit for my preview window, https://github.com/icsharpcode/AvalonEdit

## PlugIns Example

To create some plugin just reference PlugInBase and inherit from ISearchInFolderPlugIn.
You can use my plugIn SearchInTextFilesKMP as a template

      public async Task<List<FileSearchEventArgs>> SearchInFolder(
         String path,
         String extension,
         String searchTerm,
         Boolean subDirs,
         Int32 minFileSizeMB,
         Int32 maxFileSizeMB,
         IProgress<Int32> progress,
         CancellationToken cancellationToken)

* path, is the folder path where you will search for the files
* extension, extension of the files. Just one extension because in the application is a loop for multiple extensions
* searchTerm, some text
* subDirs, ture if search also in subdirs
* minFileSizeMB, it can be 0 if every file will be allowed. Idea was to limit the results
* maxFileSizeMB, it can be 0 if every file will be allowed. If that value is 0 minFileSizeMB will be also 0
* progress, is for the waiting bar
* cancellationToken, for cancelling

## Ideas/ToDo's

* (todo) add more PlugIns like PDF, Office Documents, etc
* (idea) export of the results (csv, txt, clipboard, etc. no ideas yet)
* (idea) treeview, add some columns for filesize and amount of results
* (idea) ~~Tabs~~ (I don't like that idea anymore)
* (todo) change the design like files in a different color, dark theme, etc
* (todo) add optional page number and not only row number for PDFs
* (todo) expand the tree is too slow if there are a lot of nodes
* (todo) searching in multiple folder
* (todo) PlugIns including PreViewWindow like TextFiles + TextPreViewWindow, PDF + PDF PreViewWindow

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for more details.
