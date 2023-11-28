namespace Geometry;
using System;
using Godot;

public partial class Circle : IDrawable
{
  public double Radius { get; }

  public Point Center { get; }

  public Circle()
  {
    this.Center = new Point();

    float XMin = Math.Abs((float)Center.X_Coord - IDrawable.Window_StartX);
    XMin = Math.Min(XMin, Math.Abs((float)Center.X_Coord - IDrawable.Window_EndX));

    float YMin = Math.Abs((float)Center.Y_Coord - IDrawable.Window_StartY);
    YMin = Math.Min(YMin, Math.Abs((float)Center.Y_Coord - IDrawable.Window_EndY));

    float rad = Math.Min(XMin, YMin);
    rad = Math.Max(rad, 10 * IDrawable.Point_Representation_Radius / IDrawable.ZoomFactor);

    this.Radius = rad;
  }

  public Circle(Point Center, double Radius)
  {
    this.Center = Center;
    this.Radius = Radius;
  }

  public Point Sample()
  {
    var angle = IDrawable.rnd.RandfRange(0, (float)(2 * Math.PI));

    var vector = new Point(200, 0).GetRotatedAsVector(angle);

    vector = (this.Radius / vector.Norm) * vector;

    var p = Center + vector;

    return p;
  }
}