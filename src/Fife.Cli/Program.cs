using Fife.Cli;
using Fife.Core;

return Cli.Run(args);

namespace Fife.Cli
{
    internal static class Cli
    {
        private const int ExUsage = 64;
        private const int ExDataErr = 65;
        private const int ExSoftware = 70;

        public static int Run(string[] args)
        {
            if (args.Length <= 1) return args.Length == 1 ? RunFile(args[0]) : RunPrompt();

            Console.Error.WriteLine("Usage: fife [script]");
            return ExUsage;

        }

        private static int RunFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Could not open file '{path}'.");
                return ExUsage;
            }

            FifeEngine engine = new();
            engine.Run(File.ReadAllText(path));

            if (engine.HadError) return ExDataErr;

            return engine.HadRuntimeError ? ExSoftware : 0;
        }

        private static int RunPrompt()
        {
            Console.WriteLine("fife REPL - type 'exit' to quit.");
            FifeEngine engine = new();

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line is null || line.Trim() is "exit" or "quit") break;
                if (line.Trim().Length == 0) continue;

                var value = engine.RunRepl(line);
                if (value is not null) Console.WriteLine(Interpreter.Stringify(value));

                engine.Reset();
            }

            return 0;
        }
    }
}
