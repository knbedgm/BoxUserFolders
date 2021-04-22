using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Box.V2;
using BUFHelper;
using Box.V2.Models;

namespace BoxUserFolders
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootcmd = new RootCommand
            {
                new Option<FileInfo>(new[] {"--box-auth-file", "-f" }, description: "Box JWT JSON file") {IsRequired = true },
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
            getUsersCmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile) =>
            {
                var UF = new UserFolders(makeClient(boxAuthFile));
                var list = UF.getUsers();
                list.ForEach((BoxUser user) =>
                {
                    console.Out.Write($"{user.Id}: {user.Name}\n");
                });
            });

            var getNewCmd = new Command("list-new-users");
            getNewCmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile) =>
            {
                var UF = new UserFolders(makeClient(boxAuthFile));
                var list = UF.getNewUserEvents();
                //list.Entries.ForEach((BoxEnterpriseEvent e) =>
                //{
                //    console.Out.Write($"{e.}: {e.Name}\n");
                //});

                var ad = list.Entries[0].AdditionalDetails;

                foreach (var key in ad.Keys)
                {
                    console.Out.Write($"{key}: {ad[key]}\n");
                }
            });

            var getTokenCmd = new Command("get-token");
            getTokenCmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile) =>
            {
                console.Out.Write((new BoxJWTAuth(BoxConfig.CreateFromJsonFile(boxAuthFile.OpenRead()))).AdminToken() + "\n");
            });

            var getFolderListingCmd = new Command("ls") {
                new Argument<string>("folder-id")
            };
            getFolderListingCmd.Handler = CommandHandler.Create((IConsole console, FileInfo boxAuthFile, string folderId) =>
            {
                try
                {
                    var UF = new UserFolders(makeClient(boxAuthFile, "13792189662"));
                    var list = UF.getFolderContents(folderId);
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

            return rootcmd.InvokeAsync(args).Result;
        }


        private static BoxClient makeClient(FileInfo authFile, string asUser = null)
        {
            var boxcfg = BoxConfig.CreateFromJsonFile(authFile.OpenRead());
            var jwt = new BoxJWTAuth(boxcfg);
            return jwt.AdminClient(jwt.AdminToken(), asUser: asUser);
        }
    }
}
