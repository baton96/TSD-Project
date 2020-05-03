using System;
using Esprima;
using Esprima.Utils;
using Esprima.Ast;
using System.Collections.Generic;
using BSDetector.Analysis.Repos;
using BSDetector.Resources;

namespace BSDetector
{
    /// <summary>
    /// Performs analysis of a single file
    /// </summary>
    public class CodeAnalyzer
    {
        private string fileName;
        private string code;
        private int linesAnalyzed = 0;
        private List<AstSmell> AstSmells = new List<AstSmell> { new TooManyParametersFunction(), new TooManyParametersArrowFunction() };
        private List<LineSmell> LineSmells = new List<LineSmell> { new LineTooLong() };

        /// <summary>
        /// Constructor that uses source code and filename
        /// </summary>
        /// <param name="code">Source code for analysis</param>
        /// <param name="fileName">Analyzed file name</param>
        public CodeAnalyzer(string code, string fileName = null)
        {
            this.code = code;
            this.fileName = fileName;
        }

        /// <summary>
        /// CodeAnalyzer constructor that uses a repository file to be analyzed
        /// </summary>
        /// <param name="file">Repository file to be analyzed</param>
        public CodeAnalyzer(IRepoFile file)
        {
            this.code = file.fileContent;
            this.fileName = file.fileName;
        }

        /// <summary>
        /// Simplified analysis is performed by iterating over the entire code line by line 
        /// </summary>
        private void SimplifiedAnalysis()
        {

            string[] lines = code.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None);

            int lineNum = 1;
            string previousLine = "";
            foreach (var line in lines)
            {
                foreach (var smell in LineSmells)
                {
                    smell.AnalyzeLine(line, previousLine, lineNum);
                }
                lineNum++;
                previousLine = line;
            }

            linesAnalyzed = lineNum - 1;
        }

        /// <summary>
        /// Recursively performs Depth First Traversal of the AST
        /// </summary>
        /// <param name="node">Current/starting AST node</param>
        /// <param name="depth">Current depth</param>
        private void ASTreeDFS(INode node, int depth)
        {

            foreach (var smell in AstSmells)
            {
                smell.AnalyzeNode(node, depth);
            }

            foreach (var child in node.ChildNodes)
            {
                ASTreeDFS(child, depth + 1);
            }
        }

        /// <summary>
        /// Builds an abstract syntax tree out of source file
        /// </summary>
        /// <returns>Parsed program including it's AST</returns>
        private Script BuildAST()
        {
            var customParserOptions = new ParserOptions();
            customParserOptions.Loc = true;
            customParserOptions.Range = true;

            var parser = new JavaScriptParser(code, customParserOptions);
            var program = parser.ParseScript();

            return program;
        }

        /// <summary>
        /// Ast analysis is performed by going through the entire abstract syntax tree
        /// and analyzing visited nodes
        /// </summary>
        private void ASTAnalysis()
        {

            var tree = BuildAST().Body;

            foreach (var node in tree.AsNodes())
            {
                ASTreeDFS(node, 0);
            }
        }

        /// <summary>
        /// Performs analysis of a single file
        /// </summary>
        /// <returns>Results of file analysis</returns>
        public FileAnalysisResult AnalyzeCode()
        {
            SimplifiedAnalysis();
            ASTAnalysis();
            var smellsList = new List<Smell>(LineSmells);
            smellsList.AddRange(AstSmells);
            return new FileAnalysisResult
            {
                FileName = fileName,
                LinesAnalyzed = linesAnalyzed,
                SmellsDetected = smellsList.ToArray()
            };
        }

        /// <summary>
        /// Generates an abstract syntax tree as JOSN.
        /// </summary>
        /// <returns>JSON stringified abstract syntax tree</returns>
        public string GetASTasJSONstring()
        {
            var program = BuildAST();
            return program.ToJsonString(new AstJson
                                            .Options()
                                            .WithIncludingLineColumn(true));
        }
    }
}