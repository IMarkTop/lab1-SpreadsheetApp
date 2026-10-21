using System.Numerics;
using Spreadsheet.Core.Ast;
using Spreadsheet.Core.Errors;
using Spreadsheet.Core.Lexing;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Core.Parsing;

/// <summary>Рекурсивний низхідний синтаксичний аналізатор за граматикою з <c>docs/DECISIONS.md</c>.</summary>
public sealed class Parser
{
    private const int MaxDepth = 256;

    /// <summary>Розбирає вираз; зупиняється на першій синтаксичній помилці.</summary>
    public ParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        LexResult lexed = new Lexer().Tokenize(text);
        if (!lexed.IsSuccess)
        {
            return new ParseResult(null, lexed.Error);
        }

        var state = new State(lexed.Tokens);
        try
        {
            Expr expr = ParseOr(state);
            if (state.Current.Kind != TokenKind.End)
            {
                throw Error(state.Current, "Очікувався оператор або кінець виразу");
            }

            return new ParseResult(expr, null);
        }
        catch (SyntaxFailure failure)
        {
            return new ParseResult(null, failure.Error);
        }
    }

    private static Expr ParseOr(State s)
    {
        Expr left = ParseAnd(s);
        while (s.Current.Kind == TokenKind.Or)
        {
            Token op = s.Advance();
            Expr right = ParseAnd(s);
            left = new BinaryExpr(BinaryOperator.Or, left, right, op.Position, op.Length);
        }

        return left;
    }

    private static Expr ParseAnd(State s)
    {
        Expr left = ParseNot(s);
        while (s.Current.Kind == TokenKind.And)
        {
            Token op = s.Advance();
            Expr right = ParseNot(s);
            left = new BinaryExpr(BinaryOperator.And, left, right, op.Position, op.Length);
        }

        return left;
    }

    private static Expr ParseNot(State s)
    {
        if (s.Current.Kind == TokenKind.Not)
        {
            Token op = s.Advance();
            s.Enter();
            Expr operand = ParseNot(s);
            s.Leave();
            return new UnaryExpr(UnaryOperator.Not, operand, op.Position, op.Length);
        }

        return ParseComparison(s);
    }

    private static Expr ParseComparison(State s)
    {
        Expr left = ParseAdditive(s);
        BinaryOperator? op = s.Current.Kind switch
        {
            TokenKind.Equal => BinaryOperator.Equal,
            TokenKind.Less => BinaryOperator.Less,
            TokenKind.Greater => BinaryOperator.Greater,
            _ => null,
        };

        if (op is null)
        {
            return left;
        }

        Token token = s.Advance();
        Expr right = ParseAdditive(s);
        if (s.Current.Kind is TokenKind.Equal or TokenKind.Less or TokenKind.Greater)
        {
            throw Error(s.Current, "Операції порівняння не можна записувати ланцюжком; використайте дужки");
        }

        return new BinaryExpr(op.Value, left, right, token.Position, token.Length);
    }

    private static Expr ParseAdditive(State s)
    {
        Expr left = ParseMultiplicative(s);
        while (s.Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            Token op = s.Advance();
            Expr right = ParseMultiplicative(s);
            var kind = op.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            left = new BinaryExpr(kind, left, right, op.Position, op.Length);
        }

        return left;
    }

    private static Expr ParseMultiplicative(State s)
    {
        Expr left = ParsePrimary(s);
        while (s.Current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Div or TokenKind.Mod)
        {
            Token op = s.Advance();
            Expr right = ParsePrimary(s);
            var kind = op.Kind switch
            {
                TokenKind.Star => BinaryOperator.Multiply,
                TokenKind.Slash => BinaryOperator.Divide,
                TokenKind.Div => BinaryOperator.IntegerDivide,
                _ => BinaryOperator.Modulo,
            };
            left = new BinaryExpr(kind, left, right, op.Position, op.Length);
        }

        return left;
    }

    private static Expr ParsePrimary(State s)
    {
        Token token = s.Current;
        switch (token.Kind)
        {
            case TokenKind.Number:
                s.Advance();
                return new NumberLiteral(BigInteger.Parse(token.Text), token.Position, token.Length);

            case TokenKind.True:
            case TokenKind.False:
                s.Advance();
                return new BoolLiteral(token.Kind == TokenKind.True, token.Position, token.Length);

            case TokenKind.CellRef:
                s.Advance();
                CellAddress.TryParse(token.Text, out CellAddress address);
                return new CellRefExpr(address, token.Position, token.Length);

            case TokenKind.LeftParen:
                s.Advance();
                s.Enter();
                Expr inner = ParseOr(s);
                s.Leave();
                if (s.Current.Kind != TokenKind.RightParen)
                {
                    throw Error(s.Current, "Очікувалась закриваюча дужка «)»");
                }

                s.Advance();
                return inner;

            default:
                throw Error(token, "Очікувалось число, логічне значення, посилання на клітинку або «(»");
        }
    }

    private static SyntaxFailure Error(Token at, string message) =>
        new(new ExpressionError(ErrorKind.Syntax, message, at.Position, at.Length));

    private sealed class SyntaxFailure(ExpressionError error) : Exception
    {
        public ExpressionError Error { get; } = error;
    }

    private sealed class State(IReadOnlyList<Token> tokens)
    {
        private int _index;
        private int _depth;

        public Token Current => tokens[_index];

        public Token Advance()
        {
            Token token = tokens[_index];
            if (token.Kind != TokenKind.End)
            {
                _index++;
            }

            return token;
        }

        public void Enter()
        {
            if (++_depth > MaxDepth)
            {
                throw Error(Current, "Забагато вкладених дужок або операцій not");
            }
        }

        public void Leave() => _depth--;
    }
}
