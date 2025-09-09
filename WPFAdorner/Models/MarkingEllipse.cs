using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFAdorner.Models
{
    public class MarkingEllipse:MarkingShape
    {

        private double _centerX;
        private double _centerY;
        private double _radiusY;
        private double _radiusX;
        private double _angle;
        public double CenterX
        {
            get => _centerX;
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

        public double RadiusX
        {
            get => _radiusX;
            set
            {
                if (SetProperty(ref _radiusX, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
        public double RadiusY
        {
            get => _radiusY;
            set
            {
                if (SetProperty(ref _radiusY, value))
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
            $"{RadiusX:##.000},{RadiusY:##.000};Center:{CenterX:##.000},{CenterY:##.000};Angle:{Angle:##.000}";
        public MarkingEllipse(bool isNew):base(isNew)
        {
            Name = "椭圆";
        }

    }
}
