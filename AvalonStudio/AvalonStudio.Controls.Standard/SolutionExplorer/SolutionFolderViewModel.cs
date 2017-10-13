namespace AvalonStudio.Controls.Standard.SolutionExplorer
{
    using Avalonia.Controls;
    using Avalonia.Media;
    using AvalonStudio.Extensibility;
    using AvalonStudio.MVVM;
    using AvalonStudio.Platforms;
    using AvalonStudio.Projects;
    using AvalonStudio.Shell;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class SolutionFolderViewModel : SolutionParentViewModel<ISolutionFolder>
    {
        private DrawingGroup _folderOpenIcon;
        private DrawingGroup _folderIcon;

        public SolutionFolderViewModel(ISolutionParentViewModel parent, ISolutionFolder folder) : base(folder)
        {
            Parent = parent;

            _folderIcon = "FolderIcon".GetIcon();
            _folderOpenIcon = "FolderOpenIcon".GetIcon();

            Initialise(parent);
        }

        public override DrawingGroup Icon => IsExpanded ? _folderOpenIcon : _folderIcon;
    }

    public interface ISolutionParentViewModel
    {
        bool IsExpanded { get; set; }

        ISolutionParentViewModel Parent { get; }

        void VisitChildren(Action<SolutionItemViewModel> visitor);
    }

    public abstract class SolutionParentViewModel<T> : SolutionItemViewModel<T>, ISolutionParentViewModel
        where T : ISolutionFolder
    {
        private ObservableCollection<SolutionItemViewModel> _items;
        private bool _isExpanded;

        public SolutionParentViewModel(T model) : base(model)
        {
            AddNewFolderCommand = ReactiveCommand.Create(() =>
            {
                Model.Solution.AddItem(SolutionFolder.Create("New Folder"), Model);

                Model.Solution.Save();
            });

            AddExistingProjectCommand = ReactiveCommand.Create(async () =>
            {
                var dlg = new OpenFileDialog();
                dlg.Title = "Open Project";

                var extensions = new List<string>();

                var shell = IoC.Get<IShell>();

                foreach (var projectType in shell.ProjectTypes)
                {
                    extensions.AddRange(projectType.Extensions);
                }

                dlg.Filters.Add(new FileDialogFilter { Name = "AvalonStudio Project", Extensions = extensions });

                if (Platform.PlatformIdentifier == Platforms.PlatformID.Win32NT)
                {
                    dlg.InitialDirectory = Model.Solution.CurrentDirectory;
                }
                else
                {
                    dlg.InitialFileName = Model.Solution.CurrentDirectory;
                }

                dlg.AllowMultiple = false;

                var result = await dlg.ShowAsync();

                if (result != null && !string.IsNullOrEmpty(result.FirstOrDefault()))
                {
                    var proj = AvalonStudioSolution.LoadProjectFile(Model.Solution, result[0]);

                    if (proj != null)
                    {
                        Model.Solution.AddItem(proj, Model);
                        Model.Solution.Save();
                    }
                }
            });

            AddNewProjectCommand = ReactiveCommand.Create(() =>
            {
                
            });

            RemoveCommand = ReactiveCommand.Create(() =>
            {
                Model.Solution.RemoveItem(Model);
                Model.Solution.Save();
            });
        }

        protected void Initialise(ISolutionParentViewModel parent)
        {
            Items = new ObservableCollection<SolutionItemViewModel>();
            Items.BindCollections(Model.Items, p => { return SolutionItemViewModel.Create(parent, p); }, (pvm, p) => pvm.Model == p);
        }

        public void VisitChildren(Action<SolutionItemViewModel> visitor)
        {
            foreach(var child in Items)
            {
                if(child is ISolutionParentViewModel folder)
                {
                    folder.VisitChildren(visitor);
                }

                visitor(child);
            }
        }

        public ObservableCollection<SolutionItemViewModel> Items
        {
            get
            {
                return _items;
            }

            set
            {
                _items = value; this.RaisePropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                _isExpanded = value;
                this.RaisePropertyChanged(nameof(Icon));
            }
        }

        public ReactiveCommand AddNewFolderCommand { get; private set; }
        public ReactiveCommand AddNewProjectCommand { get; private set; }
        public ReactiveCommand AddExistingProjectCommand { get; private set; }
        public ReactiveCommand RemoveCommand { get; private set; }        
    }
}