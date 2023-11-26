namespace Geometry;

using System;
// using Godot;

public partial class Arc : IDrawable
{
    public Ray Start_Ray {get;}

    public Point Center {get;}

    public double Radius {get;}
    public double Angle {get;}
 
    public Arc()
    {
        var core = new Circle();
        
        this.Start_Ray = new Ray(core.Center, new Point());
        this.Angle = IDrawable.rnd.RandfRange(0, (float)(2*Math.PI));
        this.Center = core.Center;
        this.Radius = core.Radius;
    }

    public Arc(Ray Start_Ray, Ray End_Ray, double Radius)
    {
        this.Start_Ray = Start_Ray;
        this.Center = Start_Ray.First_Point;
        this.Radius = Radius;

        Angle = Start_Ray.Director_Vector.AngleTo(End_Ray.Director_Vector);
    }

    public Point Sample()
    {
        var newAngle = IDrawable.rnd.RandfRange(0, (float)Angle);

        var vector = this.Start_Ray.Director_Vector.GetRotatedAsVector(newAngle);

        vector = (this.Radius/vector.Norm)*vector;

        return vector + this.Center;
    }
}
