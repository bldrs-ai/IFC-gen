using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IFC4.Generators;
//using NDesk.Options;
using CommandLine;
using Express;
using CommandLine.Text;

namespace IFC.Generate
{
    class Program
    {
        private static string language;
        private static string outDir;
        private static string expressPath;
        private static bool showHelp;
        private static bool outputTokens;
        private static string shortName;

        static int Main(string[] args)
        {
            bool parseOptionsResult = ParseOptions(args);

            if (!parseOptionsResult)
            {
                return showHelp ? 0 : 1;
            }

            var generators = new List<Tuple<ILanguageGenerator, IFunctionsGenerator>>();

            if (language == "bldrsts")
            {
                generators.Add(new Tuple<ILanguageGenerator, IFunctionsGenerator>(new BldrsGenerator(shortName),
                    null));

            }

            using (FileStream fs = new FileStream(expressPath, FileMode.Open))
            {
                var input = new AntlrInputStream(fs);
                var lexer = new Express.ExpressLexer(input);
                var tokens = new CommonTokenStream(lexer);

                var parser = new Express.ExpressParser(tokens);
                parser.BuildParseTree = true;

                var tree = parser.schemaDecl();
                var walker = new ParseTreeWalker();

                var testSb = new StringBuilder();

                foreach (var generator in generators)
                {
                    var listener = new Express.ExpressListener(generator.Item1);
                    walker.Walk(listener, tree);
                    Generate(listener, outDir, generator.Item1, generator.Item2);
                }

                if (!outputTokens)
                {
                    return 0;
                }

                var tokenStr = new StringBuilder();

                foreach (var t in tokens.GetTokens())
                {
                    tokenStr.AppendLine(t.ToString());
                }

                Console.WriteLine(tokenStr);
            }

            return 0;
        }

        private static void Generate(Express.ExpressListener listener, string outDir,
        ILanguageGenerator generator, IFunctionsGenerator functionsGenerator)
        {
            var names = new List<string>();

            generator.TypesData = listener.TypeData;

            var sd = listener.TypeData.Where(kvp=>kvp.Value is SelectType).
                                Select(v=>new {v.Key, v.Value}).
                                ToDictionary(t => t.Key, t => (SelectType)t.Value);

            generator.SelectData = sd;

            foreach (var kvp in listener.TypeData)
            {
                var td = kvp.Value;
                File.WriteAllText(Path.Combine(outDir, $"{td.Name}.{generator.FileExtension}"), td.ToString());
                names.Add(td.Name);
            }

            generator.GenerateManifest(outDir, names);

            if (functionsGenerator != null)
            {
                functionsGenerator.SelectData = sd;
                var functionsPath = Path.Combine(outDir, functionsGenerator.FileName);
                File.WriteAllText(functionsPath, functionsGenerator.Generate(listener.FunctionData.Values));
            }

            generator.TypesData = null;
        }

        private class Options
        {
            [Option('e', "express", HelpText = "The path the express schema.",Required = true)]
            public string ExpressPath { get; set; } = "";

            [Option('o', "output", HelpText = "The directory in which the code is generated.",Required = true)]
            public string OutputDirectory { get; set; } = "";

            [Option('l', "language", HelpText = "The target language (csharp)",Default ="csharp" )]
            public string Language { get; set; } = "csharp";

            [Option('p',"tokens", HelpText ="Output tokens to stdout during parsing.",Default = false)]
            public bool Tokens { get; set; } = false;

            [Option('h', "help", Default = false)]
            public bool ShowHelp { get; set; } = false;

            [Option('s', "shortname", HelpText = "The shortname for the schema.", Required = true)]
            public string ShortName{ get; set; } = "";
        }

        private static bool ParseOptions(string[] args)
        {
            var parserResult = CommandLine.Parser.Default.ParseArguments<Options>(args);

            return parserResult.MapResult(
                options =>
                {
                    expressPath = options.ExpressPath;
                    outDir = options.OutputDirectory;
                    language = options.Language;
                    outputTokens = options.Tokens;
                    showHelp = options.ShowHelp;
                    shortName = options.ShortName;

                    if ( showHelp )
                    {
                        return ShowHelp(parserResult);
                    }

                    return !showHelp;
                },
                _ =>
                {
                    return ShowHelp(parserResult);
                }
            );
        }

        private static bool ShowHelp< T >(ParserResult< T > result)
        {
            var helpText = HelpText.AutoBuild(result, text =>
            {
                text.AdditionalNewLineAfterOption = true;
                text.Heading = "IFC-gen (bldrs)";

                return text;
            });

            Console.WriteLine(helpText);

            return false;
        }
    }
}
