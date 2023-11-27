namespace GSharp.Expression;

using System.Collections.Generic;

public class Sequence : Expr
{
  public readonly Token openBraceTk;
  public readonly List<Expr> items;

  public Sequence(Token openBraceTk, List<Expr> items)
  {
    this.items = items;
    this.openBraceTk = openBraceTk;
  }

  public override R Accept<R>(IVisitor<R> visitor)
  {
    return visitor.VisitSequenceExpr(this);
  }
}