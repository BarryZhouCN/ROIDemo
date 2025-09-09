using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFAdorner.Models
{
    public class MarkingRect:MarkingShape
    {
        private double _centerX;
        private double _centerY;
        private double _height;
        private double _width;
        private double _angle;
        public double CenterX
        {
            get=> _centerX;
            set
            {
                if (SetProperty(ref _centerX, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public double CenterY
        {
            get => _centerY;
            set
            {
                if (SetProperty(ref _centerY, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                if (SetProperty(ref _width, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
        public double Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public double Angle
        {
            get => _angle;
            set
            {
                if (SetProperty(ref _angle, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public override string Description =>
            $"{Width:##.000}x{Height:##.000};Center:{CenterX:##.000},{CenterY:##.000};Angle:{Angle:##.000}";

        public MarkingRect():base()
        {

        }

        public MarkingRect(bool isNew):base(isNew)
        {
            Name = "矩形";
        }
    }
}
