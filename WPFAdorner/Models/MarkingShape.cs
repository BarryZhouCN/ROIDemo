using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPFAdorner.Models
{
    public class MarkingShape:ObservableObject
    {
        private bool _isSelected;
        private string _description;
        public event EventHandler IsSelectedChanged;
        public bool IsNew { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    IsSelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string Name { get ; protected set; }

        public virtual string Description { get; }

        public MarkingShape()
        {

        }

        public MarkingShape(bool isNew)
        {
            IsNew = isNew;
        }
    }
}
