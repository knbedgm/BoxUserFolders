using System;
using System.Threading.Tasks;
using Box.V2;
using Box.V2.Models;

namespace BUFHelper
{
    public class UserFolders
    {
        private BoxClient client;

        public UserFolders(BoxClient client)
        {
            this.client = client;
        }

        public System.Collections.Generic.List<Box.V2.Models.BoxUser> getUsers()
        {
            var users = client.UsersManager.GetEnterpriseUsersAsync(limit: 20);
            users.Wait();
            return users.Result.Entries;
        }

        public BoxEventCollection<BoxEnterpriseEvent> getNewUserEvents()
        {
            var events = client.EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "NEW_USER" });
            return events.Result;
        }

        public BoxEventCollection<BoxEnterpriseEvent> getGroupAddEvents()
        {
            var events = client.EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "GROUP_ADD_USER" });
            return events.Result;
        }

        public void makeFoldersForUser(string uid)
        {
            var userGroups = GetUserGroups(uid).Result;
            userGroups.Entries.ForEach((BoxGroupMembership membership) =>
            {
                if (membership.Group.Id == "123")
                {
                    
                }
            });
        }

        public async Task<BoxCollection<BoxGroupMembership>> GetUserGroups(string uid)
        {
            return await client.GroupsManager.GetAllGroupMembershipsForUserAsync(uid, autoPaginate: true);
        }

        public async Task<BoxCollection<BoxGroupMembership>> getGroupUsers(string gid)
        {
            return await client.GroupsManager.GetAllGroupMembershipsForGroupAsync(gid, autoPaginate: true);
        }

        public async Task<BoxCollection<BoxItem>> GetFolderContents(string id)
        {
            return await client .FoldersManager.GetFolderItemsAsync(id, 1000, autoPaginate: true);
        }
    }
}
