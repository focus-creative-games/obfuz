using CommandLine;

namespace DeobfuscateStackTrace
{
    internal class Program
    {
        private class CommandOptions
        {

            [Option('m', "mappingFile", Required = true, HelpText = "mapping xml file")]
            public string MappingFile { get; set; }

            [Option('i', "input", Required = true, HelpText = "input obfuscated log file")]
            public string InputFile { get; set; }

            [Option('o', "output", Required = true, HelpText = "output deobfuscated log file")]
            public string OutputFile { get; set; }

        }

        static void Main(string[] args)
        {
            CommandOptions opt = ParseArgs(args);

            if (!File.Exists(opt.MappingFile))
            {
                Console.Error.WriteLine($"Mapping file {opt.MappingFile} not found");
                Environment.Exit(1);
            }
            if (!File.Exists(opt.InputFile))
            {
                Console.Error.WriteLine($"Input file {opt.InputFile} not found");
                Environment.Exit(1);
            }
            var reader = new SymbolMappingReader(opt.MappingFile);
            StackTraceDeObfuscator.Convert(reader, opt.InputFile, opt.OutputFile);
        }

        private static CommandOptions ParseArgs(string[] args)
        {
            var helpWriter = new StringWriter();
            var parser = new Parser(settings =>
            {
                settings.AllowMultiInstance = true;
                settings.HelpWriter = helpWriter;
            });

            var result = parser.ParseArguments<CommandOptions>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                Console.Error.WriteLine(helpWriter.ToString());
                Environment.Exit(1);
            }
            return ((Parsed<CommandOptions>)result).Value;
        }
    }
}
