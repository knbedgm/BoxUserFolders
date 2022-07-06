using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Box.V2;
using Box.V2.Models;

namespace BoxUserFolders
{
	class UserFolders
	{
		private DataHolder data;

		public UserFolders(DataHolder data)
		{
			this.data = data;
		}

		public async Task<BoxCollection<BoxUser>> GetUsers()
		{
			return await data.clientManager.Get("0").UsersManager.GetEnterpriseUsersAsync(autoPaginate: true);
		}

		public async Task<BoxUser> GetUser(string uid)
		{
			return await data.clientManager.Get("0").UsersManager.GetUserInformationAsync(uid);
		}

		public async Task<bool> DeleteUser(BoxUser user)
		{
			return await DeleteUser(user.Id);
		}

		public async Task<bool> DeleteUser(string uid)
		{
			return (await data.clientManager.Get("0").UsersManager.DeleteEnterpriseUserAsync(uid, false, true)) == null;
		}

		public async Task<BoxEventCollection<BoxEnterpriseEvent>> GetNewUserEvents()
		{
			return await data.clientManager.Get("0").EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "NEW_USER" });
		}

		public async Task<BoxEventCollection<BoxEnterpriseEvent>> GetGroupAddEvents(string streamPos = null, DateTime? after = null)
		{
			return await data.clientManager.Get("0").EventsManager.EnterpriseEventsAsync(eventTypes: new[] { "GROUP_ADD_USER" }, streamPosition: streamPos, createdAfter: after);
		}

		private async Task RunBase(BoxGroupMembership membership, Config.Maping maping, BoxCollection<BoxItem> folders)
		{
			if (!UserFolderInList(membership.User, folders))
			{
				await MakeUserFolder(membership.User, maping.folderId);
			}
		}


		public async Task RunForUser(string uid)
		{
			var taskList = new List<Task>();

			var user = await GetUser(uid);
			var userGroups = GetUserGroups(uid).Result;
			userGroups.Entries.ForEach((BoxGroupMembership membership) =>
			{
				Config.Maping maping = data.config.groupMapings.Find((map) => { return map.groupId == membership.Group.Id; });
				if (maping != null)
				{
					var folders = GetFolderContents(maping.folderId).Result;
					taskList.Add(RunBase(membership, maping, folders));
				}
			});

			await Task.WhenAll(taskList);
		}

		public async Task RunForGroup(Config.Maping maping)
		{
			var taskList = new List<Task>();

			var groupUsers = await GetGroupUsers(maping.groupId);
			var folders = await GetFolderContents(maping.folderId);

			groupUsers.Entries.ForEach((BoxGroupMembership membership) =>
			{
				taskList.Add(RunBase(membership, maping, folders));
			});

			await Task.WhenAll(taskList);
		}

		public async Task RunForGroup(string gid)
		{
			Config.Maping maping = data.config.groupMapings.Find((map) => { return map.groupId == gid; });
			await RunForGroup(maping);
		}

		public bool UserFolderInList(BoxUser user, BoxCollection<BoxItem> list)
		{
			foreach (var folder in list.Entries)
			{
				if (folder.Name == user.Name)
					return true;
			}
			return false;
		}

		public async Task MakeUserFolder(BoxUser user, string folderId)
		{
			var client = GetClientForFolder(folderId);
			var bfr = new BoxFolderRequest
			{
				Name = user.Name,
				Parent = new BoxRequestEntity
				{
					Id = folderId
				}
			};
			var newFolder = await client.FoldersManager.CreateAsync(bfr);

			var bcr = new BoxCollaborationRequest
			{
				AccessibleBy = new BoxCollaborationUserRequest { Id = user.Id, Type = BoxType.user },
				CanViewPath = false,
				Role = "co-owner",
				Item = new BoxRequestEntity { Id = newFolder.Id, Type = BoxType.folder }
			};
			await client.CollaborationsManager.AddCollaborationAsync(bcr, notify: false);
			Console.WriteLine($"Made Folder \"{user.Name}\"");
		}

		public async Task<BoxCollection<BoxGroupMembership>> GetUserGroups(string uid)
		{
			return await data.clientManager.Get("0").GroupsManager.GetAllGroupMembershipsForUserAsync(uid, autoPaginate: true);
		}

		public async Task<BoxCollection<BoxGroupMembership>> GetGroupUsers(string gid)
		{
			return await data.clientManager.Get("0").GroupsManager.GetAllGroupMembershipsForGroupAsync(gid, autoPaginate: true);
		}

		public async Task<BoxCollection<BoxItem>> GetFolderContents(string id)
		{
			return await GetClientForFolder(id).FoldersManager.GetFolderItemsAsync(id, 1000, autoPaginate: true);
		}

		public BoxClient GetClientForFolder(string id)
		{
			var maping = data.config.groupMapings.Find((map) => { return map.folderId == id; });
			string uid = "0";
			if (maping != null)
			{
				uid = maping.adminId;
			}
			return data.clientManager.Get(uid);
		}

		public BoxClient GetClientForFolder(BoxItem item)
		{
			return GetClientForFolder(item.Id);
		}
	}
}
