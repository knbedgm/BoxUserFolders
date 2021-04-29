using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoxUserFolders
{

    static class ClientManager
    {
        private static readonly Dictionary<string, BoxClient> map = new Dictionary<string, BoxClient>();
        private static string admintoken;
        private static BoxJWTAuth jwt;

        public static BoxClient Get(string id)
        {
            if (jwt == null)
                throw new Exception("ClientManager uninitialized");
            id = id.Trim();
            if (!map.ContainsKey(id))
            {
                if (id == "0")
                    map.Add(id, jwt.AdminClient(admintoken));
                else
                    map.Add(id, jwt.AdminClient(admintoken, asUser: id));
            }
            return map[id];
        }

        public static void LoadConfig(FileInfo file)
        {
            var boxcfg = BoxConfig.CreateFromJsonFile(file.OpenRead());
            jwt = new BoxJWTAuth(boxcfg);
            admintoken = jwt.AdminToken();
        }
    }
}
