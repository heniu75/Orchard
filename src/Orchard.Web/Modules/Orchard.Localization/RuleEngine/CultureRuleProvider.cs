﻿using System;
using System.Globalization;
using System.Linq;
using Orchard.Events;

namespace Orchard.Localization.RuleEngine {
    public interface IRuleProvider : IEventHandler {
        void Process(dynamic ruleContext);
    }

    public class CultureRuleProvider : IRuleProvider {
        private readonly WorkContext _workContext;

        public CultureRuleProvider(WorkContext workContext) {
            _workContext = workContext;
        }

        public void Process(dynamic ruleContext) {
            if (String.Equals(ruleContext.FunctionName, "culture-code", StringComparison.OrdinalIgnoreCase)) {
                ProcessCultureCode(ruleContext);
            }

            if (String.Equals(ruleContext.FunctionName, "culture-lcid", StringComparison.OrdinalIgnoreCase)) {
                ProcessCultureId(ruleContext);
            }

            if (String.Equals(ruleContext.FunctionName, "culture-isrtl", StringComparison.OrdinalIgnoreCase)) {
                ProcessCurrentCultureIsRtl(ruleContext);
            }
        }

        private void ProcessCurrentCultureIsRtl(dynamic ruleContext) {
            var currentUserCulture = CultureInfo.GetCultureInfo(_workContext.CurrentCulture);

            var isRtl = ((object[]) ruleContext.Arguments)
                .Cast<bool>()
                .SingleOrDefault();

            ruleContext.Result = (isRtl == currentUserCulture.TextInfo.IsRightToLeft);
        }

        private void ProcessCultureCode(dynamic ruleContext) {
            var currentUserCulture = CultureInfo.GetCultureInfo(_workContext.CurrentCulture);

            ruleContext.Result = ((object[])ruleContext.Arguments)
                .Cast<string>()
                .Select(CultureInfo.GetCultureInfo)
                .Any(c => c.Name == currentUserCulture.Name);
        }

        private void ProcessCultureId(dynamic ruleContext) {
            var currentUserCulture = CultureInfo.GetCultureInfo(_workContext.CurrentCulture);

            ruleContext.Result = ((object[])ruleContext.Arguments)
                .Cast<int>()
                .Select(CultureInfo.GetCultureInfo)
                .Any(c => c.Name == currentUserCulture.Name);
        }
    }
}