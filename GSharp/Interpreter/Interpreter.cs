using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using GSharp.Exceptions;
using GSharp.Expression;
using GSharp.Objects;
using GSharp.Objects.Collections;
using GSharp.Objects.Figures;
using GSharp.Parser;
using GSharp.Statement;
using static GSharp.TokenType;

namespace GSharp.Interpreter;

// <summary>
// Interpreter for GSharp code
// </summary>

public class Interpreter : IInterpreter, Expr.IVisitor<GSObject>, Stmt.IVisitor<VoidObject>
{
  private readonly Action<RuntimeError> runtimeErrorHandler;
  private readonly GSharpEnvironment globals = new();

  internal IBindingHandler bindingHandler { get; }

  private readonly Action<string> standardOutputHandler;

  private ImmutableList<Stmt> previousStmts = ImmutableList.Create<Stmt>();
  private IEnvironment currentEnvironment;

  public Interpreter(Action<RuntimeError> runtimeErrorHandler, Action<string> standardOutputHandler, IBindingHandler? bindingHandler = null)
  {
    this.runtimeErrorHandler = runtimeErrorHandler;
    this.bindingHandler = bindingHandler ?? new BindingHandler();
    this.standardOutputHandler = standardOutputHandler;

    this.currentEnvironment = globals;
  }

  public object? Eval(string source, ScanErrorHandler scanErrorHandler, ParseErrorHandler parseErrorHandler, NameResolutionErrorHandler nameResolutionErrorHandler)
  {
    if (string.IsNullOrWhiteSpace(source))
    {
      return null;
    }

    ScanAndParseResult result = Parser.Parser.ScanAndParse(source, scanErrorHandler, parseErrorHandler);

    if (result == ScanAndParseResult.scanErrorOccurred || result == ScanAndParseResult.parseErrorEncountered)
    {
      return null;
    }

    if (result.hasStatements)
    {
      var previousAndNewStmts = previousStmts.Concat(result.statements!).ToImmutableList();

      //
      // resolving names phase

      bool hasNameResolutionErrors = false;
      var nameResolver = new NameResolver(bindingHandler, nameResolutionError =>
      {
        hasNameResolutionErrors = true;
        nameResolutionErrorHandler(nameResolutionError);
      });

      nameResolver.Resolve(previousAndNewStmts);

      if (hasNameResolutionErrors)
      {
        return null;
      }

      //
      // type validation
      //

      bool typeValidationFailed = false;

      // ...

      if (typeValidationFailed)
      {
        return null;
      }

      previousStmts = previousAndNewStmts.ToImmutableList();

      try
      {
        Interpret(result.statements!);
      }
      catch (RuntimeError e)
      {
        runtimeErrorHandler(e);
        return VoidObject.Void;
      }

      return VoidObject.Void;
    }

    throw new IllegalStateException("syntax was not list of Stmt");
  }

  public string? Parse(string source, Action<ScanError> scanErrorHandler, Action<ParseError> parseErrorHandler)
  {
    // ... 
    // scanning phase
    // ...

    bool hasScanErrors = false;
    var scanner = new Scanner(source, scanError =>
    {
      hasScanErrors = true;
      scanErrorHandler(scanError);
    });

    var tokens = scanner.ScanTokens();

    if (hasScanErrors)
    {
      // something went wrong as early as the "scan" stage
      // abort the rest of the processing
      return null;
    }

    // ...
    // parsing phase
    // ...

    bool hasParseErrors = false;
    var parser = new GSharp.Parser.Parser(tokens, parseError =>
    {
      hasParseErrors = true;
      parseErrorHandler(parseError);
    });

    object syntax = parser.ParseStmts();

    if (hasParseErrors)
    {
      // one or more parse error were encountered
      // they have been reported upstream
      // so we just abort the evaluation at this stage
      return null;
    }

    if (syntax is List<Stmt> stmts)
    {
      StringBuilder result = new();

      throw new NotImplementedException();
    }
    else
    {
      throw new IllegalStateException($"syntax expected to be List<Stmt>, not {syntax}");
    }
  }

  private void Interpret(IEnumerable<Stmt> statements)
  {
    foreach (var statement in statements)
    {
      try
      {
        Execute(statement);
      }
      catch (TargetInvocationException ex)
      {
        string message = ex.InnerException?.Message ?? ex.Message;

        throw new RuntimeError(null, message);
      }
      catch (SystemException ex)
      {
        throw new RuntimeError(null, ex.Message, ex);
      }
    }
  }

  public GSObject VisitLiteralExpr(Literal expr)
  {
    if (expr.Value is INumericLiteral parseNumber)
    {
      double value = (double)parseNumber.value;
      return new Objects.Scalar(value);
    }
    else if (expr.Value is bool parseBoolean)
    {
      return new Objects.Scalar(parseBoolean);
    }
    else if (expr.Value is string parseString)
    {
      return new Objects.String(parseString);
    }
    else if (expr.Value is null)
    {
      return new Objects.Undefined();
    }
    else
    {
      throw new ArgumentException($"Invalid literal found {expr}");
    }
  }

  public GSObject VisitLogicalExpr(Logical expr)
  {
    GSObject left = Evaluate(expr.Left)!;

    if (expr.Oper.type == OR)
    {
      if (left.GetTruthValue())
      {
        return new Objects.Scalar(true);
      }

      bool right = Evaluate(expr.Right)!.GetTruthValue();

      return new Scalar(right);
    }
    else if (expr.Oper.type == AND)
    {
      if (!left.GetTruthValue())
      {
        return new Objects.Scalar(false);
      }

      bool right = (bool)Evaluate(expr.Right)!.GetTruthValue();

      return new Scalar(right);
    }
    else
    {
      throw new RuntimeError(expr.Oper, $"Unsupported logical operator: {expr.Oper.type}");
    }
  }

  public GSObject VisitUnaryExpr(Unary expr)
  {
    GSObject right = Evaluate(expr.Right);

    switch (expr.Oper.type)
    {
      case NOT:
        return new Scalar(!right.GetTruthValue());
      case MINUS:
        return IOperate<Mult>.Operate(right, new Scalar(-1));
      default:
        throw new RuntimeError(expr.Oper, $"Unsupported operator encountered: {expr.Oper.type}");
    }
  }

  public GSObject VisitVariableExpr(Variable expr)
  {
    return LookUpVariable(expr.Name, expr);
  }

  private GSObject LookUpVariable(Token name, Variable identifier)
  {
    if (bindingHandler.GetLocalBinding(identifier, out Binding? localBinding))
    {
      if (localBinding is IDistanceBinding distanceBinding)
      {
        return currentEnvironment.GetAt(distanceBinding.Distance - 1, name.lexeme);
      }
      else
      {
        throw new RuntimeError(name, $"Attempting to lookup variable for non-distance-aware biding '{localBinding}'");
      }
    }
    else
    {
      return globals.Get(name);
    }
  }

  public GSObject VisitGroupingExpr(Grouping expr)
  {
    return Evaluate(expr.Expression);
  }

  private GSObject Evaluate(Expr expr)
  {
    try
    {
      return expr.Accept(this);
    }
    catch (TargetInvocationException ex)
    {
      Token? token = (expr as IToken)?.Token;

      string message = ex.InnerException?.Message ?? ex.Message;

      throw new RuntimeError(token, message, ex);
    }
    catch (SystemException ex)
    {
      Token? token = (expr as IToken)?.Token;

      throw new RuntimeError(token, ex.Message, ex);
    }
  }

  private void Execute(Stmt stmt)
  {
    stmt.Accept(this);
  }

  public void ExecuteBlock(IEnumerable<Stmt>? statements, IEnvironment blockEnvironment)
  {
    IEnvironment previousEnvironment = currentEnvironment;

    try
    {
      currentEnvironment = blockEnvironment;

      foreach (var statement in statements)
      {
        Execute(statement);
      }
    }
    finally
    {
      currentEnvironment = previousEnvironment;
    }
  }

  private GSObject ExecuteInternalBlock(IEnumerable<Stmt>? statements, Expr body, IEnvironment blockEnvironment)
  {
    IEnvironment previousEnvironment = currentEnvironment;

    GSObject result;
    try
    {
      currentEnvironment = blockEnvironment;

      if (statements is not null)
      {
        foreach (var statement in statements)
        {
          Execute(statement);
        }
      }

      result = Evaluate(body);
    }
    finally
    {
      currentEnvironment = previousEnvironment;
    }

    return result;
  }

  public GSObject VisitBinaryExpr(Binary expr)
  {
    GSObject left = Evaluate(expr.Left);
    GSObject right = Evaluate(expr.Right);

    switch (expr.Oper.type)
    {
      case GREATER:
        bool IsGreater = !(IOperate<LessTh>.Operate(left, right).GetTruthValue() || left.Equals(right));
        return new Scalar(IsGreater);
      case GREATER_EQUAL:
        bool IsGreaterOrEqual = !IOperate<LessTh>.Operate(left, right).GetTruthValue();
        return new Scalar(IsGreaterOrEqual);
      case LESS:
        return IOperate<LessTh>.Operate(left, right);
      case LESS_EQUAL:
        bool IsLessOrEqual = IOperate<LessTh>.Operate(left, right).GetTruthValue() || left.Equals(right);
        return new Scalar(IsLessOrEqual);
      case NOT_EQUAL:
        return new Scalar(!left.Equals(right));
      case EQUAL_EQUAL:
        return new Scalar(left.Equals(right));
      case PLUS:
        return IOperate<Add>.Operate(left, right);
      case MINUS:
        return IOperate<Subst>.Operate(left, right);
      case MUL:
        return IOperate<Mult>.Operate(left, right);
      case DIV:
        return IOperate<Div>.Operate(left, right);
      case MOD:
        return IOperate<Mod>.Operate(left, right);
      case POWER:
        throw new NotImplementedException();
      default:
        string message = InterpreterMessages.UnsupportedOperandTypes(expr.Oper.type, left, right);
        throw new RuntimeError(expr.Oper, message);
    }
  }

  public GSObject VisitCallExpr(Call expr)
  {
    GSObject calle = Evaluate(expr.Calle);

    var arguments = new List<GSObject>();
    foreach (var argument in expr.Arguments)
    {
      arguments.Add(Evaluate(argument));
    }

    switch (calle)
    {
      case ICallable callable:
        if (arguments.Count != callable.Arity())
        {
          throw new RuntimeError(expr.Paren, "Expected" + callable.Arity() + " argument(s) but got " + arguments.Count + ".");
        }

        try
        {
          return callable.Call(this, arguments);
        }
        catch (RuntimeError)
        {
          throw;
        }
        catch (Exception e)
        {
          if (expr.Calle is Variable variable)
          {
            throw new RuntimeError(variable.Name, $"{variable.Name.lexeme}: {e.Message}");
          }
          else
          {
            throw new RuntimeError(expr.Paren, e.Message);
          }
        }
      default:
        throw new RuntimeError(expr.Paren, $"Can only call functions and native methods, not {calle}");
    }
  }

  public GSObject VisitConditionalExpr(Conditional expr)
  {
    bool condition = Evaluate(expr.Condition)!.GetTruthValue();

    if (condition)
    {
      return Evaluate(expr.ThenBranch);
    }
    else
    {
      return Evaluate(expr.ElseBranch);
    }
  }

  public GSObject VisitEmptyExpr(Empty expr)
  {
    return new Objects.Undefined();
  }

  public GSObject VisitIntRangeExpr(IntRange expr)
  {
    if (expr.Right is null)
    {
      return new GeneratorSequence(new IntRangeGenerator((int)expr.Left.literal));
    }
    else
    {
      List<GSObject> rangeInt = new();
      for (int i = (int)expr.Left.literal; i <= (int)expr.Right.literal; i++)
      {
        rangeInt.Add(new Scalar(i));
      }

      return new FiniteStaticSequence(rangeInt);
    }
  }

  public GSObject VisitLetInExpr(LetIn expr)
  {
    return ExecuteInternalBlock(expr.Stmts, expr.Body, currentEnvironment);
  }

  public GSObject VisitSequenceExpr(Expression.Sequence expr)
  {
    throw new NotImplementedException();
  }

  public GSObject VisitUndefinedExpr(Expression.Undefined expr)
  {
    return new Objects.Undefined();
  }

  public VoidObject VisitColorStmt(ColorStmt stmt)
  {
    throw new NotImplementedException();
  }

  public VoidObject VisitConstantStmt(ConstantStmt stmt)
  {
    GSObject value = Evaluate(stmt.Initializer);

    if (value is Objects.Collections.Sequence valueSeq)
    {
      int cntConsts = stmt.Names.Count;
      for (int i = 0; i < cntConsts - 1; i++)
      {
        currentEnvironment.Define(stmt.Names[i], valueSeq[i]);
      }

      currentEnvironment.Define(stmt.Names.Last(), valueSeq.GetRemainder(cntConsts - 1));
    }
    else
    {
      if (stmt.Names.Count != 1)
      {
        throw new RuntimeError(stmt.Token, "Cannot assign some constants to unique value.");
      }

      currentEnvironment.Define(stmt.Names[0], value);
    }

    return VoidObject.Void;
  }

  public VoidObject VisitDrawStmt(Draw stmt)
  {
    throw new NotImplementedException();
  }

  public VoidObject VisitExpressionStmt(ExpressionStmt stmt)
  {
    Evaluate(stmt.Expression);
    return VoidObject.Void;
  }

  public VoidObject VisitFunctionStmt(Function stmt)
  {
    var function = new GSFunction(stmt, currentEnvironment);
    currentEnvironment.Define(stmt.Name, function);
    return VoidObject.Void;
  }

  public VoidObject VisitImportStmt(Import stmt)
  {
    throw new NotImplementedException();
  }

  public VoidObject VisitPrintStmt(Print stmt)
  {
    GSObject? value = Evaluate(stmt.Expression);
    if (stmt.Label is not null)
    {
      standardOutputHandler(stmt.Label.lexeme + ": " + value.ToString());
    }
    else
    {
      standardOutputHandler(value.ToString());
    }

    return VoidObject.Void;
  }

  public VoidObject VisitRestoreStmt(Restore stmt)
  {
    throw new NotImplementedException();
  }

  public VoidObject VisitReturnStmt(Statement.Return stmt)
  {
    GSObject value = null;
    if (stmt.Value != null)
    {
      value = Evaluate(stmt.Value);
    }

    throw new Exceptions.Return(value);
  }

  public VoidObject VisitVarStmt(Var stmt)
  {
    GSObject value;

    if (stmt.Initializer is not null)
    {
      value = Evaluate(stmt.Initializer);
    }
    else
    {
      switch (stmt.TypeReference.TypeSpecifier.type)
      {
        case POINT:
          value = new Point();
          break;
        case LINE:
          value = new Line();
          break;
        case SEGMENT:
          value = new Segment();
          break;
        case RAY:
          value = new Ray();
          break;
        case CIRCLE:
          value = new Circle();
          break;
        case ARC:
          value = new Arc();
          break;
        case POINT_SEQUENCE:
          value = new GeneratorSequence(new RandomFigureGenerator(FigureOptions.Point));
          break;
        case LINE_SEQUENCE:
        case CIRCLE_SEQUENCE:
        case RAY_SEQUENCE:
        case SEGMENT_SEQUENCE:
        case ARC_SEQUENCE:
          throw new NotImplementedException();
        default:
          throw new ArgumentException("Unsupported variable type used.");
      }
    }

    currentEnvironment.Define(stmt.Name, value);
    return VoidObject.Void;
  }
}