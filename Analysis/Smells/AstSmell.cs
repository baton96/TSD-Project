using Esprima.Ast;
using Esprima;

namespace BSDetector
{
    public abstract class AstSmell : Smell
    {
        public virtual void AnalyzeNode(INode node, int depth)
        {

        }
        public void RegisterOccurence(Location loc)
        {
            RegisterOccurence(loc.Start.Line, loc.Start.Column, loc.End.Line, loc.End.Column);
        }
    }
}