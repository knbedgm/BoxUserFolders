using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Box.V2;
using Box.V2.Models;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Newtonsoft.Json;

namespace BoxUserFolders
{
	class Program
	{
		static int Main(string[] args)
		{
			var boxcfgopt = new Option<FileInfo>(new[] { "--config", "-c" }, description: "UserFolders JSON config file") { IsRequired = true, AllowMultipleArgumentsPerToken = false };
			var boxfileopt = new Option<FileInfo>(new[] { "--box-auth-file", "-a" }, description: "Box JWT JSON file") { IsRequired = false, AllowMultipleArgumentsPerToken = false };

			var writecfgopt = new Option<bool>(new[] { "--write-default-config" }, description: "OVerwrites the provided config with the default") { IsRequired = false, AllowMultipleArgumentsPerToken = false };

			var rootcmd = new RootCommand
			{
				boxcfgopt,
				boxfileopt,
				writecfgopt,
			};

			//rootcmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile) =>
			//{
			//    if (boxAuthFile != null)
			//    {
			//        console.Out.Write(boxAuthFile.FullName);
			//        console.Out.Write("\n");
			//    }
			//    else
			//    {
			//        console.Out.Write("please provide an auth file\n");
			//    }
			//});

			var getUsersCmd = new Command("list-users");
			getUsersCmd.Handler = CommandHandler.Create((IConsole console) =>
			{
				var list = UserFolders.GetUsers().Result.Entries;
				list.ForEach((BoxUser user) =>
				{
					console.Out.Write($"{user.Id}: {user.Name}\n");
				});
			});

			var getNewCmd = new Command("list-new-users");
			getNewCmd.Handler = CommandHandler.Create((IConsole console) =>
			{
				var list = UserFolders.GetNewUserEvents().Result;
				//list.Entries.ForEach((BoxEnterpriseEvent e) =>
				//{
				//    console.Out.Write($"{e.}: {e.Name}\n");
				//});

				//var ad = list.Entries[0].AdditionalDetails;
				foreach (var ent in list.Entries)
				{

					if (ent.Source is BoxUser)
					{
						BoxUser usr = (BoxUser)ent.Source;
						console.Out.WriteLine($"{ent.CreatedAt}: {usr.Name}");
					}
				}

				//console.Out.WriteLine(list.Entries[0].Source.GetType().ToString());

			});

			var getTokenCmd = new Command("get-token");
			getTokenCmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile) =>
			{
				console.Out.Write((new BoxJWTAuth(BoxConfig.CreateFromJsonFile(boxAuthFile.OpenRead()))).AdminToken() + "\n");
			});

			var getFolderListingCmd = new Command("ls") {
				new Argument<string>("folder-id")
			};
			getFolderListingCmd.Handler = CommandHandler.Create((IConsole console, string folderId) =>
			{
				try
				{
					var list = UserFolders.GetFolderContents(ClientManager.Get("957394352"), folderId).Result;
					list.Entries.ForEach((BoxItem item) =>
					{
						console.Out.Write($"{item.Id}: {item.Name}\n");
					});
				}
				catch (Exception e)
				{
					console.Error.Write(e.ToString() + "\n");
					throw;
				}

			});


			rootcmd.AddCommand(getUsersCmd);
			rootcmd.AddCommand(getNewCmd);
			rootcmd.AddCommand(getTokenCmd);
			rootcmd.AddCommand(getFolderListingCmd);

			var clBuilder = new CommandLineBuilder(rootcmd);
			clBuilder.UseDefaults();
			clBuilder.UseMiddleware(async (context, next) =>
			{

				if (context.ParseResult.Errors.Count > 0)
				{
					await next(context);
					return;
				}

				bool overwriteConfig = context.ParseResult.ValueForOption<bool>(writecfgopt);
				var cfgfile = context.ParseResult.ValueForOption<FileInfo>(boxcfgopt);
				Config config;

				if (overwriteConfig)
				{
					Config newcfg = new Config()
					{
						accessFile = "./boxaccesfile.json",
						groupAddedPointer = "0",
					};
					newcfg.groupMapings.Add("123", new Config.Maping() { folderId = "456", adminId = "789" });
					config = newcfg;

					var ser = new JsonSerializer
					{
						Formatting = Formatting.Indented
					};

					if (cfgfile.Exists)
						cfgfile.Delete();

					var writer = new JsonTextWriter(new StreamWriter(cfgfile.OpenWrite()));
					ser.Serialize(writer, config);
					writer.Flush();
				}
				else
				{
					if (!cfgfile.Exists)
					{
						context.Console.Error.WriteLine("Provided config file does not exist!");
						context.ExitCode = 2;
						return;
					}

					var ser = new JsonSerializer();
					var reader = new JsonTextReader(cfgfile.OpenText());
					config = ser.Deserialize<Config>(reader);
				}

				var authFileProvided = false;
				//var flagauthfile = context.ParseResult.ValueForOption<FileInfo>("--box-auth-file");
				var flagauthfile = context.ParseResult.ValueForOption<FileInfo>(boxfileopt);
				if (flagauthfile != null)
					authFileProvided = true;

				FileInfo authfile = authFileProvided ? flagauthfile : new FileInfo(config.accessFile); // TODO: load from config file

				if (authfile.Exists)
				{
					ClientManager.LoadConfig(authfile);
				}
				else
				{
					context.Console.Error.WriteLine("Provided auth file does not exist!");
					context.ExitCode = 1;
					return;
				}

				await next(context);
			});

			var parser = clBuilder.Build();

			return parser.InvokeAsync(args).Result;
		}

	}
}
