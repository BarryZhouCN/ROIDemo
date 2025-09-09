using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFAdorner.Models;

namespace WPFAdorner.Helpers
{
    public static class PositionCalHelper
    {
        public static List<Point> CalculateVertices(double width, double height, Point center, double rotationAngleDegrees)
        {
            // 将角度转换为弧度
            double angle = rotationAngleDegrees * Math.PI / 180.0;
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            // 计算未旋转前的四个顶点（相对于中心点）
            double halfWidth = width / 2;
            double halfHeight = height / 2;

            Point[] unrotatedPoints = new Point[]
            {
                new Point(-halfWidth, -halfHeight), // 左上（相对坐标）
                new Point(halfWidth, -halfHeight),  // 右上（相对坐标）
                new Point(halfWidth, halfHeight),   // 右下（相对坐标）
                new Point(-halfWidth, halfHeight),   // 左下（相对坐标）
                new Point(0,-(halfHeight+25))
            };

            // 旋转并平移点
            List<Point> rotatedPoints = new List<Point>();

            foreach (Point point in unrotatedPoints)
            {
                // 应用旋转矩阵
                double rotatedX = point.X * cosAngle - point.Y * sinAngle;
                double rotatedY = point.X * sinAngle + point.Y * cosAngle;

                // 平移到中心点位置
                rotatedPoints.Add(new Point(rotatedX + center.X, rotatedY + center.Y));
            }

            return rotatedPoints;
        }

        public static double CalculateAngle(Point a, Point b,Point center)
        {
            Vector vectorA = a - center;
            Vector vectorB = b - center;
            // 计算向量OA和OB之间的角度
            double angleA = Math.Atan2(vectorA.Y, vectorA.X) * (180 / Math.PI);
            double angleB = Math.Atan2(vectorB.Y, vectorB.X) * (180 / Math.PI);

            // 计算划过的角度
            double sweptAngle = angleB - angleA;

            return sweptAngle;
        }
    }
}
