﻿using System;
using Spark.Parser.Markup;

namespace SparkSense.Parsing
{
    public enum SparkSyntaxTypes
    {
        None,
        Tag,
        Attribute,
        Variable,
        Invalid,
    }

    public class SparkSyntax
    {
        private ITextExplorer _textExplorer;

        public SparkSyntax(ITextExplorer textExplorer)
        {
            _textExplorer = textExplorer;
        }

        public bool IsSparkSyntax(char inputCharacter, out SparkSyntaxTypes syntaxType)
        {
            syntaxType = SparkSyntaxTypes.None;
            if (inputCharacter.Equals(char.MinValue)) return false;
            syntaxType = GetSyntaxType(inputCharacter);

            return syntaxType != SparkSyntaxTypes.None;
        }

        public SparkSyntaxTypes GetSyntaxType(char key)
        {
            switch (key)
            {
                case '<':
                    return SparkSyntaxTypes.Tag;
                case ' ':
                    return CheckForAttribute();
                default:
                    if (Char.IsLetterOrDigit(key.ToString(), 0))
                        return SparkSyntaxTypes.Variable;
                    return SparkSyntaxTypes.None;
            }
        }
        private SparkSyntaxTypes CheckForAttribute()
        {
            var node = _textExplorer.GetNodeAtPosition(_textExplorer.GetStartPosition());
            return node is ElementNode ? SparkSyntaxTypes.Attribute : SparkSyntaxTypes.None;
        }
    }
}
