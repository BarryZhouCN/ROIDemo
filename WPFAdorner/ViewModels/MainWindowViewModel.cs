using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WPFAdorner.Helpers;
using WPFAdorner.Models;

namespace WPFAdorner.ViewModels
{
    public class MainWindowViewModel:ObservableObject
    {
        private WriteableBitmap _bitmap;
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set
            {
                if (SetProperty(ref _bitmap, value))
                {

                }
            }
        }

        public ObservableCollection<MarkingShape> Adorners { get; set; } = new ObservableCollection<MarkingShape>();
        public ICommand SelectImageFileCommand { get; }
        public ICommand AddRectCommand { get; }
        public ICommand AddEllipseCommand { get; }
        public ICommand AddFreeFormCommand { get; }

        public MainWindowViewModel()
        {
            SelectImageFileCommand = new RelayCommand(OnSelectImageFileCommandExecuted);
            AddRectCommand = new RelayCommand(OnAddRectCommandExecuted);
            AddEllipseCommand = new RelayCommand(OnAddEllipseCommandExecuted);
            AddFreeFormCommand = new RelayCommand(OnAddFreeFormCommandExecuted);
        }

        private void OnAddFreeFormCommandExecuted()
        {
            Adorners.Add(new MarkingFreeForm());
        }

        private void OnAddEllipseCommandExecuted()
        {
            Adorners.Add(new MarkingEllipse(true));
        }

        private void OnAddRectCommandExecuted()
        {
            Adorners.Add(new MarkingRect(true));
        }

        private void OnSelectImageFileCommandExecuted()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "图像文件|*.jpg;*.png;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                Bitmap image = new Bitmap(dialog.FileName);
                Bitmap = BitmapHelper.BitmapToWriteableBitmap(image);
                image.Dispose();
            }
        }
    }
}
