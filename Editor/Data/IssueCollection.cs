using System.Collections.Generic;

namespace Triband.Validation.Editor.Data
{
    public struct IssueCollection
    {
        public readonly List<Issue> issues;

        public IssueCollection(List<Issue> checks, string inspectedStage)
        {
            this.issues = new List<Issue>(checks);

            foreach (var check in checks)
            {
                check.ResolveParent(checks);
            }
            
            inspectedStageInfoText = inspectedStage;
        }
        
        public static IssueCollection empty => new IssueCollection(null, "");

        public readonly string inspectedStageInfoText;
    }
}