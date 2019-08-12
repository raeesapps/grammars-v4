using Antlr4.Runtime;
using System.Collections.Generic;

public abstract class PhpBaseLexer : Lexer
{
    public bool AspTags = true;
    bool _scriptTag;
    bool _styleTag;
    string _heredocIdentifier;
    int _prevTokenType;
    string _htmlNameText;
    bool _phpScript;
    bool _insideString;

    public override IToken NextToken()
    {
        CommonToken token = (CommonToken)base.NextToken();

        if (token.Type == PHPEnd || token.Type == PHPEndSingleLineComment)
        {
            if (_mode == SingleLineCommentMode)
            {
                // SingleLineCommentMode for such allowed syntax:
                // <?php echo "Hello world"; // comment ?>
                PopMode(); // exit from SingleLineComment mode.
            }
            PopMode(); // exit from PHP mode.

            if (string.Equals(token.Text, "</script>", System.StringComparison.Ordinal))
            {
                _phpScript = false;
                token.Type = ScriptClose;
            }
            else
            {
                // Add semicolon to the end of statement if it is absente.
                // For example: <?php echo "Hello world" ?>
                if (_prevTokenType == SemiColon || _prevTokenType == Colon
                    || _prevTokenType == OpenCurlyBracket || _prevTokenType == CloseCurlyBracket)
                {
                    token.Channel = SkipChannel;
                }
                else
                {
                    token.Type = SemiColon;
                }
            }
        }
        else if (token.Type == HtmlName)
        {
            _htmlNameText = token.Text;
        }
        else if (token.Type == HtmlDoubleQuoteString)
        {
            if (string.Equals(token.Text, "php", System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_htmlNameText, "language"))
            {
                _phpScript = true;
            }
        }
        else if (_mode == HereDoc)
        {
            // Heredoc and Nowdoc syntax support: http://php.net/manual/en/language.types.string.php#language.types.string.syntax.heredoc
            switch (token.Type)
            {
                case StartHereDoc:
                case StartNowDoc:
                    _heredocIdentifier = token.Text.Substring(3).Trim().Trim('\'');
                    break;

                case HereDocText:
                    if (CheckHeredocEnd(token.Text))
                    {
                        PopMode();

                        var heredocIdentifier = GetHeredocIdentifier(token.Text);
                        if (token.Text.Trim().EndsWith(";"))
                        {
                            token.Text = heredocIdentifier + ";\n";
                            token.Type = SemiColon;
                        }
                        else
                        {
                            token = (CommonToken)base.NextToken();
                            token.Text = heredocIdentifier + "\n;";
                        }
                    }
                    break;
            }
        }
        else if (_mode == PHP)
        {
            if (_channel != Hidden)
            {
                _prevTokenType = token.Type;
            }
        }

        return token;
    }

    protected string GetHeredocIdentifier(string text)
    {
        text = text.Trim();
        bool semi = text.Length > 0 ? text[text.Length - 1] == ';' : false;
        return semi ? text.Substring(0, text.Length - 1) : text;
    }

    protected bool CheckHeredocEnd(string text)
    {
        return string.Equals(GetHeredocIdentifier(text), _heredocIdentifier, System.StringComparison.Ordinal);
    }

    protected bool IsNewLineOrStart(int pos)
    {
        return _input.La(pos) <= 0 || _input.La(pos) == '\r' || _input.La(pos) == '\n';
    }

    protected void PushModeOnHtmlClose()
    {
        PopMode();
        if (_scriptTag)
        {
            if (!_phpScript)
            {
                PushMode(SCRIPT);
            }
            else
            {
                PushMode(PHP);
            }
            _scriptTag = false;
        }
        else if (_styleTag)
        {
            PushMode(STYLE);
            _styleTag = false;
        }
    }

    protected bool HasAspTags()
    {
        return AspTags;
    }

    protected bool HasPhpScriptTag()
    {
        return _phpScript;
    }

    protected void PopModeOnCurlyBracketClose()
    {
        if (_insideString)
        {
            _insideString = false;
            Channel = SkipChannel;
            PopMode();
        }
    }

    protected bool ShouldPushHereDocMode(pos)
    {
        return _input.La(pos) == '\r' || _input.La(pos) == '\n';
    }

    protected bool IsCurlyDollar(pos) {
        return _input.La(pos) == '$';
    }

    protected void SetInsideString() {
        _insideString = true;
    }
}