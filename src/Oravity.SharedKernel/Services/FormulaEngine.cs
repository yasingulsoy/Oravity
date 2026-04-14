namespace Oravity.SharedKernel.Services;

/// <summary>
/// Fiyat formülü değerlendirici (SPEC §FİYATLANDIRMA).
/// Desteklenen yapılar:
///   - Değişkenler:   ISAK, TDB, CARI, SUT, vb. (büyük harf)
///   - Sayısal literal: 1000, 0.90, 1,10 (TR virgül ondalık)
///   - Aritmetik:     +  -  *  /
///   - Karşılaştırma: == != > >= &lt; &lt;=
///   - Ternary:       condition ? trueExpr : falseExpr
///   - Gruplama:      (expr)
///   - İç içe ternary: ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0.80
/// </summary>
public class FormulaEngine
{
    private string _input = string.Empty;
    private int    _pos;
    private Dictionary<string, decimal> _vars = [];

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Formülü verilen değişkenlerle değerlendirir.
    /// </summary>
    /// <param name="formula">Formül metni, ör. "ISAK==1 ? TDB*0.90 : CARI*0.80"</param>
    /// <param name="variables">Değişken adı → değer</param>
    public decimal Evaluate(string formula, Dictionary<string, decimal> variables)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new FormulaException("Formül boş olamaz.");

        _input = formula
            .Replace(',', '.')  // TR ondalık virgülü → nokta
            .Trim();
        _pos  = 0;
        _vars = variables ?? [];

        var result = ParseTernary();
        SkipWhitespace();
        if (_pos < _input.Length)
            throw new FormulaException($"Beklenmeyen karakter '{_input[_pos]}' pozisyon {_pos}.");
        return result;
    }

    // ─── Parser ──────────────────────────────────────────────────────────
    private decimal ParseTernary()
    {
        var cond = ParseComparison();

        SkipWhitespace();
        if (Peek() == '?')
        {
            Consume('?');
            var trueVal  = ParseTernary();
            SkipWhitespace();
            Consume(':');
            var falseVal = ParseTernary();
            return cond != 0 ? trueVal : falseVal;
        }
        return cond;
    }

    private decimal ParseComparison()
    {
        var left = ParseAdditive();

        SkipWhitespace();
        string? op = null;
        if (Match("=="))      op = "==";
        else if (Match("!=")) op = "!=";
        else if (Match(">=")) op = ">=";
        else if (Match("<=")) op = "<=";
        else if (Match(">"))  op = ">";
        else if (Match("<"))  op = "<";

        if (op is null) return left;

        var right = ParseAdditive();
        return op switch
        {
            "==" => left == right ? 1m : 0m,
            "!=" => left != right ? 1m : 0m,
            ">=" => left >= right ? 1m : 0m,
            "<=" => left <= right ? 1m : 0m,
            ">"  => left >  right ? 1m : 0m,
            "<"  => left <  right ? 1m : 0m,
            _    => throw new FormulaException($"Bilinmeyen operatör: {op}")
        };
    }

    private decimal ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (true)
        {
            SkipWhitespace();
            if (Peek() == '+') { _pos++; left += ParseMultiplicative(); }
            else if (Peek() == '-') { _pos++; left -= ParseMultiplicative(); }
            else break;
        }
        return left;
    }

    private decimal ParseMultiplicative()
    {
        var left = ParseUnary();
        while (true)
        {
            SkipWhitespace();
            if (Peek() == '*') { _pos++; left *= ParseUnary(); }
            else if (Peek() == '/')
            {
                _pos++;
                var divisor = ParseUnary();
                if (divisor == 0) throw new FormulaException("Sıfıra bölme hatası.");
                left /= divisor;
            }
            else break;
        }
        return left;
    }

    private decimal ParseUnary()
    {
        SkipWhitespace();
        if (Peek() == '-') { _pos++; return -ParsePrimary(); }
        if (Peek() == '+') { _pos++; return ParsePrimary(); }
        return ParsePrimary();
    }

    private decimal ParsePrimary()
    {
        SkipWhitespace();
        var ch = Peek();

        // Gruplandırma
        if (ch == '(')
        {
            _pos++;
            var val = ParseTernary();
            SkipWhitespace();
            Consume(')');
            return val;
        }

        // Sayısal literal
        if (char.IsDigit(ch) || ch == '.')
            return ParseNumber();

        // Değişken
        if (char.IsLetter(ch) || ch == '_')
            return ParseVariable();

        throw new FormulaException(
            $"Beklenmeyen karakter '{ch}' pozisyon {_pos}.");
    }

    private decimal ParseNumber()
    {
        var start = _pos;
        while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
            _pos++;

        var raw = _input[start.._pos];
        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var val))
            throw new FormulaException($"Geçersiz sayı: '{raw}'");
        return val;
    }

    private decimal ParseVariable()
    {
        var start = _pos;
        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
            _pos++;

        var name = _input[start.._pos];

        // Yerleşik fonksiyonlar: MIN(a, b) ve MAX(a, b)
        SkipWhitespace();
        if (Peek() == '(' && (name is "MIN" or "MAX"))
        {
            _pos++; // '(' tüket
            var a = ParseTernary();
            SkipWhitespace();
            Consume(',');
            var b = ParseTernary();
            SkipWhitespace();
            Consume(')');
            return name == "MIN" ? Math.Min(a, b) : Math.Max(a, b);
        }

        if (!_vars.TryGetValue(name, out var val))
            throw new UnknownVariableException(name);
        return val;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    private char Peek() => _pos < _input.Length ? _input[_pos] : '\0';

    private void Consume(char expected)
    {
        SkipWhitespace();
        if (_pos >= _input.Length || _input[_pos] != expected)
            throw new FormulaException($"Beklenen '{expected}' pozisyon {_pos}.");
        _pos++;
    }

    private bool Match(string token)
    {
        if (_pos + token.Length > _input.Length) return false;
        if (!_input.AsSpan(_pos, token.Length).SequenceEqual(token)) return false;
        // Operatör önünde harf/rakam varsa == değişken adı parçasıdır
        if (token.All(char.IsLetter) && _pos + token.Length < _input.Length
            && char.IsLetterOrDigit(_input[_pos + token.Length]))
            return false;
        _pos += token.Length;
        return true;
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            _pos++;
    }
}

// ─── Exceptions ──────────────────────────────────────────────────────────
public class FormulaException : Exception
{
    public FormulaException(string message) : base(message) { }
}

public class UnknownVariableException : FormulaException
{
    public string VariableName { get; }
    public UnknownVariableException(string name)
        : base($"Bilinmeyen değişken: '{name}'. Formüle geçirilmedi.") => VariableName = name;
}
