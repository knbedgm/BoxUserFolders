using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoxUserFolders
{
	[JsonObject]
	class Config
	{
		[JsonProperty]
		public List<Maping> groupMapings = new List<Maping>();

		[JsonProperty]
		public string accessFile;

		[JsonProperty("internal.lastpointer")]
		public string groupAddedPointer;
		
		[JsonObject]

		public class Maping
		{
			[JsonProperty]
			public string groupId;

			[JsonProperty]
			public string folderId;

			[JsonProperty]
			public string adminId;
		}

		public static Config MakeDefault()
		{
			var cfg = new Config
			{
				accessFile = "./boxaccesfile.json",
				groupAddedPointer = "0",
			};
			cfg.groupMapings.Add(new Config.Maping() { groupId = "123", folderId = "456", adminId = "789" });

			return cfg;
		}
	}
}
