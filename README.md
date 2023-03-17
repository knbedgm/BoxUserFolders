# BoxUserFolders

A configurable tool to automatically create user home folders in Box as users are provisioned. 

Able to create user homes in different folders based on user group.

## Usage:
```
BoxUserFolders

Usage:
  BoxUserFolders [options] [command]

Options:
  -c, --config <config> (REQUIRED)     UserFolders JSON config file
  -a, --box-auth-file <box-auth-file>  Box JWT JSON file
  --write-default-config               Overwrites the provided config with the default
  --version                            Show version information
  -?, -h, --help                       Show help and usage information

Commands:
  list-users
  list-new-users
  get-token
  ls <folder-id>
  users-for-group <group-id>
  run-for-user <user-id>
  run-for-group <group-id>
  run
```
