using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFAdorner.Models
{
    public class MarkingFreeForm:MarkingShape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public MarkingFreeForm()
        {
            Name = "自定义";
        }
    }
}
