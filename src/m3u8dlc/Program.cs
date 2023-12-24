using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Cli;

namespace m3u8dlc
{
	internal class Program
	{
		public static async Task<n32> Main(string[] args)
		{
			CommandApp<AsyncMainCommand> app = new CommandApp<AsyncMainCommand>();
			app.Configure(configuration);
			n32 nResult = await app.RunAsync(args);
			return nResult;
		}

		private static void configuration(IConfigurator config)
		{
			_ = config.SetExceptionHandler(exceptionHandler);
			_ = config.SetApplicationName("m3u8dlc");
			_ = config.UseStrictParsing();
			_ = config.CaseSensitivity(CaseSensitivity.All);
		}

		private static n32 exceptionHandler(Exception ex)
		{
			AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
			return -1;
		}
	}

	public class AsyncMainCommand : AsyncCommand<AsyncMainCommand.Settings>
	{
		public class Settings : CommandSettings
		{
		}

		public override async Task<n32> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
		{
			return 0;
		}
	}
}
