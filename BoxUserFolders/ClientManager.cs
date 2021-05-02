using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoxUserFolders
{

    class ClientManager
    {
        private readonly Dictionary<string, BoxClient> map = new Dictionary<string, BoxClient>();
        private string admintoken;
        private BoxJWTAuth jwt;

        public BoxClient Get(string id)
        {
            id = id.Trim();
            if (!map.ContainsKey(id))
            {
                if (id == "0" || id == null)
                    map.Add(id, jwt.AdminClient(admintoken));
                else
                    map.Add(id, jwt.AdminClient(admintoken, asUser: id));
            }
            return map[id];
        }

        public static ClientManager FromConfigFile(FileInfo file)
        {
            var boxcfg = BoxConfig.CreateFromJsonFile(file.OpenRead());
            var jwt = new BoxJWTAuth(boxcfg);
            return new ClientManager(jwt);
        }

        public ClientManager(BoxJWTAuth jwt)
		{
            this.jwt = jwt;
            this.admintoken = this.jwt.AdminToken();
		}
    }
}
