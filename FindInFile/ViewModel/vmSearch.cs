using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FindInFile.Model;

namespace FindInFile.ViewModel
{
   public class vmSearch : INotifyPropertyChanged
   {
      private modelSearch _model;

      public vmSearch()
      {
         _model = new modelSearch();
      }

      public string Title
      {
         get { return _model.Title; }
         set
         {
            if (_model.Title != value)
            {
               _model.Title = value;
               OnPropertyChanged();
            }
         }
      }

      public string SearchText
      {
         get { return _model.SearchText; }
         set
         {
            if (_model.SearchText != value)
            {
               _model.SearchText = value;
               OnPropertyChanged();
            }
         }
      }

      public string Extension
      {
         get { return _model.Extension; }
         set
         {
            if (_model.Extension != value)
            {
               _model.Extension = value;
               OnPropertyChanged();
            }
         }
      }

      public string Path
      {
         get { return _model.Path; }
         set
         {
            if (_model.Path != value)
            {
               _model.Path = value;
               OnPropertyChanged();
            }
         }
      }

      public bool SubDirs
      {
         get { return _model.SubDirs; }
         set
         {
            if (_model.SubDirs != value)
            {
               _model.SubDirs = value;
               OnPropertyChanged();
            }
         }
      }
      
      public UInt32 MaxPreviewSize
      {
         get { return _model.MaxPreViewSize; }
         set
         {
            if (_model.MaxPreViewSize != value)
            {
               _model.MaxPreViewSize = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchExtensions
      {
         get { return _model.SearchExtensions; }
         set
         {
            if (_model.SearchExtensions != value)
            {
               _model.SearchExtensions = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchPaths
      {
         get { return _model.SearchPaths; }
         set
         {
            if (_model.SearchPaths != value)
            {
               _model.SearchPaths = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchTexts
      {
         get { return _model.SearchTexts; }
         set
         {
            if (_model.SearchTexts != value)
            {
               _model.SearchTexts = value;
               OnPropertyChanged();
            }
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}