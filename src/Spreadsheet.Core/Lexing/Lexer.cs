using Spreadsheet.Core.Errors;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Core.Lexing;

/// <summary>Лексичний аналізатор виразів.</summary>
public sealed class Lexer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["and"] = TokenKind.And,
        ["or"] = TokenKind.Or,
        ["not"] = TokenKind.Not,
        ["div"] = TokenKind.Div,
        ["mod"] = TokenKind.Mod,
        ["true"] = TokenKind.True,
        ["false"] = TokenKind.False,
    };

    /// <summary>Розбиває рядок на лексеми; зупиняється на першій помилці.</summary>
    public LexResult Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c is ' ' or '\t')
            {
                i++;
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                int start = i;
                while (i < text.Length && char.IsAsciiLetterOrDigit(text[i]))
                {
                    i++;
                }

                string word = text[start..i];
                if (!TryClassifyWord(word, out TokenKind kind))
                {
                    return Fail(new ExpressionError(
                        ErrorKind.Syntax, $"Недопустиме слово «{word}»", start, word.Length));
                }

                tokens.Add(new Token(kind, word, start));
                continue;
            }

            TokenKind? symbol = c switch
            {
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '=' => TokenKind.Equal,
                '<' => TokenKind.Less,
                '>' => TokenKind.Greater,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                _ => null,
            };

            if (symbol is null)
            {
                return Fail(new ExpressionError(ErrorKind.Syntax, $"Недопустимий символ «{c}»", i, 1));
            }

            tokens.Add(new Token(symbol.Value, c.ToString(), i));
            i++;
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length));
        return new LexResult(tokens, null);
    }

    private static bool TryClassifyWord(string word, out TokenKind kind)
    {
        if (word.All(char.IsAsciiDigit))
        {
            kind = TokenKind.Number;
            return true;
        }

        if (Keywords.TryGetValue(word, out kind))
        {
            return true;
        }

        if (CellAddress.TryParse(word, out _))
        {
            kind = TokenKind.CellRef;
            return true;
        }

        return false;
    }

    private static LexResult Fail(ExpressionError error) => new(Array.Empty<Token>(), error);
}
