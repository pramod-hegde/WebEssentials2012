﻿using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("HoverOrderErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class HoverOrderErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (item.Text.TrimStart(':').StartsWith("-"))
                return ItemCheckResult.Continue;

            ParseItem next = item.NextSibling;

            if (next != null)
            {
                if (next.Text.StartsWith(":") && item.IsPseudoElement() && !next.IsPseudoElement())
                {
                    string error = string.Format(Resources.ValidationPseudoOrder, item.Text, next.Text);
                    context.AddError(new SimpleErrorTag(item, error, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed));
                }

                else if (!next.Text.StartsWith(":") && item.AfterEnd == next.Start)
                {
                    string error = string.Format(Resources.BestPracticePseudosAfterOtherSelectors, next.Text);
                    context.AddError(new SimpleErrorTag(next, error));
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new[] 
                { 
                    typeof(PseudoClassSelector),
                    typeof(PseudoClassFunctionSelector),
                    typeof(PseudoElementFunctionSelector),
                    typeof(PseudoElementSelector),
                };
            }
        }
    }
}
