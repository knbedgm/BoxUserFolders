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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BoxUserFolders
{
	class Program
	{
		static DataHolder data;
		static UserFolders userFolders;

		static int Main(string[] args)
		{
			var boxcfgopt = new Option<FileInfo>(new[] { "--config", "-c" }, description: "UserFolders JSON config file") { IsRequired = true, AllowMultipleArgumentsPerToken = false };
			var boxfileopt = new Option<FileInfo>(new[] { "--box-auth-file", "-a" }, description: "Box JWT JSON file") { IsRequired = false, AllowMultipleArgumentsPerToken = false };

			var writecfgopt = new Option<bool>(new[] { "--write-default-config" }, description: "Overwrites the provided config with the default") { IsRequired = false, AllowMultipleArgumentsPerToken = false };

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
				var list = userFolders.GetUsers().Result.Entries;
				list.ForEach((BoxUser user) =>
				{
					console.Out.Write($"{user.Id}: {user.Name}\n");
				});
			});

			var getNewCmd = new Command("list-new-users");
			getNewCmd.Handler = CommandHandler.Create((IConsole console) =>
			{
				var list = userFolders.GetNewUserEvents().Result;
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
					var list = userFolders.GetFolderContents(folderId).Result;
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

			var getGroupUsersCmd = new Command("users-for-group") {
				new Argument<string>("group-id")
			};
			getGroupUsersCmd.Handler = CommandHandler.Create((IConsole console, string groupId) =>
			{
				try
				{
					var list = userFolders.GetGroupUsers(groupId).Result.Entries;

					if (list.Count > 0)
						console.Out.Write(list[0].Group.Name + ":\n\n");
					else
						console.Out.WriteLine("Group has no members.");
					
					list.ForEach((BoxGroupMembership item) =>
					{
						console.Out.WriteLine($"{item.User.Id}: {item.User.Name}");
					});
				}
				catch (Exception e)
				{
					console.Error.Write(e.ToString() + "\n");
					throw;
				}

			});

			var runForUserCmd = new Command("run-for-user") {
				new Argument<string>("user-id")
			};
			runForUserCmd.Handler = CommandHandler.Create(async (IConsole console, string userId) =>
			{
				try
				{
					await userFolders.RunForUser(userId);
				}
				catch (Exception e)
				{
					console.Error.Write(e.ToString() + "\n");
					throw;
				}

			});

			var runForGroupCmd = new Command("run-for-group") {
				new Argument<string>("group-id")
			};
			runForGroupCmd.Handler = CommandHandler.Create(async (IConsole console, string groupId) =>
			{
				try
				{
					await userFolders.RunForGroup(groupId);
				}
				catch (Exception e)
				{
					console.Error.Write(e.ToString() + "\n");
					throw;
				}

			});

			var runForAllCmd = new Command("run");
			runForAllCmd.Handler = CommandHandler.Create(async (IConsole console, string groupId) =>
			{
				try
				{
					var taskList = new List<Task>();

					foreach (var maping in data.config.groupMapings)
					{
						taskList.Add(userFolders.RunForGroup(maping));
					}

					await Task.WhenAll(taskList);
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
			rootcmd.AddCommand(getGroupUsersCmd);
			rootcmd.AddCommand(runForUserCmd);
			rootcmd.AddCommand(runForGroupCmd);
			rootcmd.AddCommand(runForAllCmd);

			var clBuilder = new CommandLineBuilder(rootcmd);
			clBuilder.UseDefaults();
			clBuilder.UseMiddleware(async (context, next) =>
			{

				//Exit if there are parse errors
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
					// Generate the default config
					config = Config.MakeDefault();

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

				ClientManager cm;

				if (authfile.Exists)
				{
					cm = ClientManager.FromConfigFile(authfile);
				}
				else
				{
					context.Console.Error.WriteLine("Provided auth file does not exist!");
					context.ExitCode = 1;
					return;
				}

				data = new DataHolder
				{
					config = config,
					clientManager = cm
				};

				userFolders = new UserFolders(data);

				await next(context);
			});

			var parser = clBuilder.Build();

			return parser.InvokeAsync(args).Result;
		}

	}
}
