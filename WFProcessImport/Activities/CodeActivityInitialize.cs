using System.Activities;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{

    public sealed class CodeActivityInitialize : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        public InArgument<int> OperationType { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

        }
    }
}
