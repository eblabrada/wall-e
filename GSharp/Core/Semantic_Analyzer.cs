namespace GSharp.Core;

using System;
using System.Linq;
using System.Collections.Generic;
using GSharp.Expression;

public class Semantic_Analyzer : Expr.IVisitor<GSharpType?>, Statement.Stmt.IVisitor<GSharpType?>
{
	private readonly Token PlaceholderTok = new(TokenType.UNDEFINED, "PLACEHOLDER", null, -1, -1);

	private const string SEMANTIC = "SEMANTIC";
	private List<Statement.Stmt> Statements;
	private ILogger Logger;

	private Context BuiltIns;
	private Context CurrentContext;

	public Semantic_Analyzer(ILogger Logger, List<Statement.Stmt> Statements)
	{
		void DefineBuiltIns()
		{
			const string POINT = "point";
			const string LINE = "line";
			const string RAY = "ray";
			const string SEGMENT = "segment";
			const string CIRCLE = "circle";
			const string ARC = "arc";
			const string MEASURE = "measure";
			const string INTERSECT = "intersect";
			const string COUNT = "count";
			const string RANDOMS = "randoms";
			const string POINTS = "points"; // sample figure
			const string SAMPLES = "samples";

			BuiltIns.Define(POINT, new Fun_Symbol(
				POINT,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Scalar), "x"),
					(new Constant_SimpleType(GSharpType.Types.Scalar), "y")
				},
				new Constant_SimpleType(GSharpType.Types.Point)
			), 2);

			BuiltIns.Define(LINE, new Fun_Symbol(
				LINE,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "p1"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p2")
				},
				new Constant_SimpleType(GSharpType.Types.Line)
			), 2);

			BuiltIns.Define(RAY, new Fun_Symbol(
				RAY,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "p1"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p2")
				},
				new Constant_SimpleType(GSharpType.Types.Ray)
			), 2);

			BuiltIns.Define(SEGMENT, new Fun_Symbol(
				SEGMENT,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "p1"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p2")
				},
				new Constant_SimpleType(GSharpType.Types.Segment)
			), 2);

			BuiltIns.Define(CIRCLE, new Fun_Symbol(
				CIRCLE,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "c"),
					(new Constant_SimpleType(GSharpType.Types.Measure), "r")
				},
				new Constant_SimpleType(GSharpType.Types.Circle)
			), 2);

			BuiltIns.Define(ARC, new Fun_Symbol(
				ARC,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "p1"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p2"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p3"),
					(new Constant_SimpleType(GSharpType.Types.Measure), "m")
				},
				new Constant_SimpleType(GSharpType.Types.Arc)
			), 4);

			BuiltIns.Define(MEASURE, new Fun_Symbol(
				MEASURE,
				new List<(GSharpType, string)>{
					(new Constant_SimpleType(GSharpType.Types.Point), "p1"),
					(new Constant_SimpleType(GSharpType.Types.Point), "p2")
				},
				new Constant_SimpleType(GSharpType.Types.Measure)
			), 2);

			BuiltIns.Define(INTERSECT, new Fun_Symbol(
				INTERSECT,
				new List<(GSharpType, string)>{
					(new Drawable_Type(), "f1"),
					(new Drawable_Type(), "f2")
				},
				new Sequence_Type(new Constant_SimpleType(GSharpType.Types.Point))
			), 2);


			BuiltIns.Define(COUNT, new Fun_Symbol(
				COUNT,
				new List<(GSharpType, string)>{
					(new Sequence_Type(new Undefined_Type()), "s")
				},
				new Constant_SimpleType(GSharpType.Types.Scalar)
			), 1);

			BuiltIns.Define(RANDOMS, new Fun_Symbol(
				RANDOMS,
				new(),
				new Sequence_Type(new Constant_SimpleType(GSharpType.Types.Scalar))
			), 0);

			BuiltIns.Define(POINTS, new Fun_Symbol(
				POINTS,
				new List<(GSharpType, string)>{
					(new Drawable_Type(), "f")
				},
				new Sequence_Type(new Constant_SimpleType(GSharpType.Types.Point))
			), 1);

			BuiltIns.Define(SAMPLES, new Fun_Symbol(
				SAMPLES,
				new(),
				new Sequence_Type(new Constant_SimpleType(GSharpType.Types.Point))
			), 0);

		}

		BuiltIns = new();

		DefineBuiltIns();

		CurrentContext = new();
		this.Statements = Statements;
		this.Logger = Logger;
	}

	public void Analyze()
	{
		foreach (var stmt in Statements)
		{
			if (stmt is not null) TypeCheck(stmt);
		}
	}

	public GSharpType? VisitVarStmt(Statement.Var var_stmt)
	{
		GSharpType Type = var_stmt.type.type switch
		{
			TokenType.POINT => new Constant_SimpleType(GSharpType.Types.Point),
			TokenType.LINE => new Constant_SimpleType(GSharpType.Types.Line),
			TokenType.RAY => new Constant_SimpleType(GSharpType.Types.Ray),
			TokenType.SEGMENT => new Constant_SimpleType(GSharpType.Types.Segment),
			TokenType.CIRCLE => new Constant_SimpleType(GSharpType.Types.Circle),
			TokenType.ARC => new Constant_SimpleType(GSharpType.Types.Arc),
			_ => null
		} ?? throw new Exception("VARIABLE DECLARATION TYPE UNSUPPORTED");

		if (var_stmt.isSequence)
			Type = new Sequence_Type(Type);

		if (!CurrentContext.Define(var_stmt.name.lexeme, new Variable_Symbol(Type, var_stmt.name.lexeme)))
			Logger.Error(SEMANTIC, var_stmt.name, "Variable " + var_stmt.name.lexeme + " Redefined");

		return null;
	}

	public GSharpType? VisitPrintStmt(Statement.Print print)
	{
		foreach (var expr in print.printe)
			TypeCheck(expr);

		return null;
	}

	private GSharpType? TypeCheck(Statement.Stmt stmt)
	{
		return stmt.Accept(this);
	}

	private GSharpType? TypeCheck(Expr expr)
	{
		return expr.Accept(this);
	}

	public GSharpType? VisitFunctionStmt(Statement.Function function)
	{

		#region check if function is defined
		if (BuiltIns.Get_Symbol(function.name.lexeme, function.Arity) != null ||
			CurrentContext.Get_Symbol(function.name.lexeme, function.Arity) != null)
		{
			Logger.Error(
				SEMANTIC,
				function.name,
				$"A function called {function.name.lexeme} with {function.Arity} parameters is already defined in this Context"
			);

			return null;
		}

		#endregion

		Context Function_Context = new();

		#region define function in inner context, get Function_Symbol


		List<(GSharpType type, string name)> Parameters = new();

		foreach (var param in function.parameters)
		{
			(GSharpType type, string name) parameter_info = (new Undefined_Type(), param.lexeme);

			Parameters.Add(parameter_info);

			Function_Context.Define(
				parameter_info.name,
				new Variable_Symbol(new Undefined_Type(), parameter_info.name)
			);
		}

		Undefined_Type return_type = new();

		Fun_Symbol Function_Symbol = new(
			function.name.lexeme,
			Parameters,
			return_type
		);

		Function_Context.Define(
			function.name.lexeme,
			Function_Symbol,
			Parameters.Count
		);

		#endregion

		var ogContext = CurrentContext;

		CurrentContext = Function_Context;

		var resolved_ret_type = TypeCheck(function.body);

		Function_Symbol = new(
			function.name.lexeme,
			Parameters,
			resolved_ret_type
		);

		CurrentContext = ogContext;

		CurrentContext.Define(Function_Symbol.Name, Function_Symbol, function.Arity);

		return null;
	}

	public GSharpType? VisitColorStmt(Statement.Color color) => null;

	public GSharpType? VisitConstantStmt(Statement.Constant constant)
	{
		GSharpType ValueType = TypeCheck(constant.initializer);

		void RedefinedError(Token VarNanme)
		{
			Logger.Error(SEMANTIC, VarNanme, "Variable " + VarNanme.lexeme + " Redefined");
		}

		bool IsUnderscore(Token name)
			=> name.lexeme == "_";

		if (ValueType is Sequence_Type seq)
		{
			for (int i = 0; i < constant.constNames.Count - 1; i++)
			{
				if (IsUnderscore(constant.constNames[i])) continue;
				if (!CurrentContext.Define(
						constant.constNames[i].lexeme,
						new Variable_Symbol(seq.Type, constant.constNames[i].lexeme)
					))
					RedefinedError(constant.constNames[i]);

			}

			if (!IsUnderscore(constant.constNames.Last()) && !CurrentContext.Define(
					constant.constNames.Last().lexeme,
					new Variable_Symbol(seq, constant.constNames.Last().lexeme)
				))
				RedefinedError(constant.constNames.Last());
		}

		else if (ValueType is Constant_SimpleType simpleType)
		{
			if (constant.constNames.Count > 1)
				Logger.Error(SEMANTIC, constant.constNames[0], "Cannot destructure a non-sequence object");

			if (!IsUnderscore(constant.constNames[0]) && !CurrentContext.Define(
					constant.constNames[0].lexeme,
					new Variable_Symbol(simpleType, constant.constNames[0].lexeme)
				))
				RedefinedError(constant.constNames[0]);

			for (int i = 1; i < constant.constNames.Count; i++)
			{
				if (IsUnderscore(constant.constNames[i])) continue;
				if (!CurrentContext.Define(
						constant.constNames[i].lexeme,
						new Variable_Symbol(new Undefined_Type(), constant.constNames[i].lexeme)
					))
					RedefinedError(constant.constNames[i]);
			}
		}

		else if (ValueType is Undefined_Type u)
		{
			foreach (var VarName in constant.constNames)
			{
				if (!IsUnderscore(VarName) &&
					!CurrentContext.Define(
						VarName.lexeme,
						new Variable_Symbol(u, VarName.lexeme)
					))
					RedefinedError(VarName);
			}
		}

		else if (ValueType is Drawable_Type d)
		{
			foreach (var VarName in constant.constNames)
			{
				if (!IsUnderscore(VarName) &&
					!CurrentContext.Define(
						VarName.lexeme,
						new Variable_Symbol(d, VarName.lexeme)
					))
					RedefinedError(VarName);
			}
		}

		return null;
	}

	public GSharpType? VisitDrawStmt(Statement.Draw draw)
	{
		Drawable_Type drawable = new();

		var expType = TypeCheck(draw.elements);
		if (!expType.Matches(drawable))
			Logger.Error(SEMANTIC, draw.commandTk, $"Object to draw must be drawable, {expType} passed instead");

		return null;
	}

	public GSharpType? VisitExpressionStmt(Statement.Expression expression) => TypeCheck(expression);

	public GSharpType? VisitImportStmt(Statement.Import import) => null;

	public GSharpType? VisitRestoreStmt(Statement.Restore restore) => null;

	public GSharpType? VisitLetInExpr(LetIn letIn)
	{
		var ogContext = CurrentContext;
		CurrentContext = new(CurrentContext);

		foreach (var instruction in letIn.instructions)
			TypeCheck(instruction);

		var return_type = TypeCheck(letIn.body);

		CurrentContext = ogContext;

		return return_type;
	}

	public GSharpType? VisitConditionalExpr(Conditional conditional)
	{
		var condition_Expr_T = TypeCheck(conditional.condition);

		if (!condition_Expr_T.Matches(new Constant_SimpleType(GSharpType.Types.Boolean)))
			Logger.Error(SEMANTIC, conditional.ifTk, $"Conditional Expression must be boolean, {condition_Expr_T} Passed instead");

		var then_branch_T = TypeCheck(conditional.thenBranch);
		var else_branch_T = TypeCheck(conditional.elseBranch);

		if (!then_branch_T.Matches(else_branch_T))
		{
			Logger.Error(SEMANTIC, conditional.ifTk, $"Conditional Expression must have matching return types, {then_branch_T} and {else_branch_T} returned instead");
			return new Undefined_Type();
		}

		return then_branch_T;
	}

	public GSharpType? VisitLiteralExpr(Literal literal)
	{
		if (literal.value is bool) return new Constant_SimpleType(GSharpType.Types.Boolean);
		else if (literal.value is double) return new Constant_SimpleType(GSharpType.Types.Scalar);
		else if (literal.value is string) return new Constant_SimpleType(GSharpType.Types.String);

		else throw new Exception("LITERAL NOT SUPPORTED");
	}

	public GSharpType? VisitBinaryExpr(Binary binary)
	{
		GSharpType TLeft = TypeCheck(binary.left);
		GSharpType TRight = TypeCheck(binary.right);

		void Operator_Doesnt_Support_Operands_Error()
		{
			Logger.Error(SEMANTIC, binary.oper, $"Operator {binary.oper.lexeme} does not support operands of type {TLeft} and {TRight} ");
		}

		Constant_SimpleType numeric = new(GSharpType.Types.Scalar);
		Constant_SimpleType measure = new(GSharpType.Types.Measure);
		bool error = false;


		switch (binary.oper.type)
		{
			case TokenType.PLUS:
				if (!TLeft.IsAddable())
					error = true;

				if (!TRight.IsAddable())
					error = true;

				if (!error)
				{
					if (!TLeft.Matches(TRight))
					{
						Operator_Doesnt_Support_Operands_Error();
						return new Undefined_Type();
					}

					if (TLeft is Undefined_Type)
						return TRight;

					return TLeft;
				}

				Operator_Doesnt_Support_Operands_Error();
				return new Undefined_Type();

			case TokenType.MINUS:
				if (!TLeft.IsSubstractable())
					error = true;

				if (!TRight.IsSubstractable())
					error = true;

				if (!error)
				{
					if (!TLeft.Matches(TRight))
					{
						Operator_Doesnt_Support_Operands_Error();
						return new Undefined_Type();
					}

					if (TLeft is Undefined_Type)
						return TRight;

					return TLeft;
				}

				Operator_Doesnt_Support_Operands_Error();
				return new Undefined_Type();


			case TokenType.EQUAL_EQUAL:
			case TokenType.NOT_EQUAL:
				return new Constant_SimpleType(GSharpType.Types.Boolean);

			case TokenType.LESS_EQUAL:
			case TokenType.LESS:
			case TokenType.GREATER:
			case TokenType.GREATER_EQUAL:

				if (!TLeft.IsComparable())
					error = true;

				if (!TRight.IsComparable())
					error = true;

				if (!TLeft.Matches(TRight))
					error = true;

				if (error) Operator_Doesnt_Support_Operands_Error();
				return new Constant_SimpleType(GSharpType.Types.Boolean);

			case TokenType.DIV:

				if (!TLeft.Is_Dividable_By(TRight))
				{
					Operator_Doesnt_Support_Operands_Error();
					return new Undefined_Type();
				}

				return TLeft;

			case TokenType.MUL:

				if (!TLeft.Is_Multiplyable_By(TRight))
				{
					Operator_Doesnt_Support_Operands_Error();
					return new Undefined_Type();
				}

				return (TLeft is Constant_SimpleType CST && CST.Type == GSharpType.Types.Scalar) ? TRight : TLeft;


			case TokenType.POWER:
			case TokenType.MOD:

				if (!TLeft.Matches(numeric) || !TRight.Matches(numeric))
					Operator_Doesnt_Support_Operands_Error();

				return numeric;

			default: throw new Exception("BINARY OPERATOR UNSUPPORTED");
		}

	}

	public GSharpType? VisitCallExpr(Call call)
	{
		var nameTok = ((Variable)call.calle).name;

		var Fun_Symbol = BuiltIns.Get_Symbol(nameTok.lexeme, call.Arity) ?? CurrentContext.Get_Symbol(nameTok.lexeme, call.Arity);

		if (Fun_Symbol == null)
		{
			Logger.Error(SEMANTIC, nameTok, $"Function {nameTok.lexeme} with {call.Arity} parameter{((call.Arity > 1) ? "s" : "")} is Undefined in this Context");
			return new Undefined_Type();
		}

		for (int i = 0; i < Fun_Symbol.Parameters.Count; i++)
		{
			var symbolParam_T = Fun_Symbol.Parameters[i].Type;
			var callArg_T = TypeCheck(call.arguments[i]);

			if (!symbolParam_T.Matches(callArg_T))
			{
				Logger.Error(
					SEMANTIC,
					nameTok,
					$"Parameter #{i + 1} of {nameTok.lexeme} must be {symbolParam_T}, {callArg_T} passed instead"
				);
			}
		}

		return Fun_Symbol.ReturnType;
	}

	public GSharpType? VisitLogicalExpr(Logical logical)
	{

		GSharpType TLeft = TypeCheck(logical.left);
		GSharpType TRight = TypeCheck(logical.right);

		Constant_SimpleType boolean = new(GSharpType.Types.Boolean);

		if (!TLeft.Matches(boolean))
		{
			if (!TRight.Matches(boolean))
			{
				Logger.Error(SEMANTIC, logical.oper, $"Logical Operator {logical.oper.lexeme} does not support {TLeft} and {TRight} operands");
			}

			Logger.Error(SEMANTIC, logical.oper, $"Logical Operator {logical.oper.lexeme} does not Support left {TLeft} operand");

		}

		if (!TRight.Matches(boolean))
			Logger.Error(SEMANTIC, logical.oper, $"Logical Operator {logical.oper.lexeme} does not Support right {TLeft} operand");

		return boolean;
	}

	public GSharpType? VisitIntRangeExpr(IntRange range)
	{
		if (range.left.literal is not double)
		{
			throw new Exception("RANGE LITERAL NOT PARSED AS NUMBER");
		}

		const string ERROR_MESSAGE = "Range Limit Must be Integer";

		if (!double.IsInteger((double)range.left.literal))
			Logger.Error(SEMANTIC, range.left, ERROR_MESSAGE);

		if (!double.IsInteger((double)range.right.literal))
			Logger.Error(SEMANTIC, range.right, ERROR_MESSAGE);

		return new Constant_SimpleType(GSharpType.Types.Scalar);
	}

	public GSharpType? VisitGroupingExpr(Grouping expr)
	{
		return TypeCheck(expr.expression);
	}

	public GSharpType? VisitSequenceExpr(Sequence sequence)
	{
		List<GSharpType> element_Types = new(sequence.items.Count);
		GSharpType MostRestrainedType = new Undefined_Type();
		bool error = false;
		bool type_restrained = false;

		for (int i = 0; i < sequence.items.Count; i++)
		{
			var currType = TypeCheck(sequence.items[i]);
			element_Types.Add(currType);
			if (MostRestrainedType is not Constant_SimpleType && !currType.IsUndefined())
			{
				if (currType is Constant_SimpleType CST)
					MostRestrainedType = CST;
				if (MostRestrainedType.IsUndefined()) MostRestrainedType = currType;

				type_restrained = true;
			}
		}

		if (type_restrained)
		{
			for (int i = 0; i < element_Types.Count; i++)
			{
				if (!error && !MostRestrainedType.Matches(element_Types[i]))
				{
					Logger.Error(SEMANTIC, sequence.openBraceTk, "Sequence declared with differently typed elements");
					error = true;
				}
			}
		}

		if (!error) return new Sequence_Type(MostRestrainedType);
		return new Sequence_Type(new Undefined_Type());
	}

	public GSharpType? VisitUnaryExpr(Unary unary)
	{
		GSharpType TRight = TypeCheck(unary.right);
		switch (unary.oper.type)
		{
			case TokenType.NOT:
				Constant_SimpleType boolean = new(GSharpType.Types.Boolean);
				if (!TRight.Matches(boolean))
					Logger.Error(SEMANTIC, unary.oper, $"Logical NOT must have boolean value as right operand {TRight} not supported");

				return boolean;
			case TokenType.MINUS:
				Constant_SimpleType scalar = new(GSharpType.Types.Scalar);
				Constant_SimpleType measure = new(GSharpType.Types.Measure);

				if (TRight.Matches(scalar)) return scalar;
				if (TRight.Matches(measure)) return measure;

				Logger.Error(SEMANTIC, unary.oper, $"Minus Unary Operator does not suppot {TRight} as Right Operand");
				return new Undefined_Type();

			default: throw new Exception("UNARY OPERATOR NOT SUPPORTED");
		}
	}

	public GSharpType? VisitUndefinedExpr(Undefined undefined) => new Undefined_Type();

	public GSharpType? VisitVariableExpr(Variable variable)
	{
		var symbol = CurrentContext.Get_Symbol(variable.name.lexeme);
		if (symbol == null)
		{
			Logger.Error(SEMANTIC, variable.name, $"Variable {variable.name.lexeme} is Undefined in this Context");
			return new Undefined_Type();
		}

		return symbol.Type;
	}

}


