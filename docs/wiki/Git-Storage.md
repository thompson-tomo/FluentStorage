<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/raw.png" width="128" align="right"></img> To use this, you need to reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.Git.svg)](https://www.nuget.org/packages/FluentStorage.Git/) package first, which wraps [LibGit2Sharp](https://github.com/libgit2/libgit2sharp/).

This package lets you read and write files in a folder (or the root) of a git repository. The repository is cloned into a local working directory and every FluentStorage operation is performed against that working tree.

> **Note**: This package targets `net8.0` and `net9.0` only, because LibGit2Sharp 0.32 does not ship a `netstandard2.0` build.

## Connect to a Git repository

```csharp
// HTTPS with username + password / personal access token
IStore storage = GitStorage.FromCredentials("https://github.com/me/repo.git", "username", "password");

// Personal access token (GitHub/GitLab/Azure DevOps)
IStore storage = GitStorage.FromToken("https://github.com/me/repo.git", "ghp_xxx");

// Full control over branch, root folder, commit author and credentials
IStore storage = GitStorage.FromUrl("https://github.com/me/repo.git", new GitStorageOptions {
   Token = "ghp_xxx",
   Branch = "develop",
   RootPath = "data",                       // work only within the "data" folder
   CommitAuthorName = "My App",
   CommitAuthorEmail = "app@example.com",
});
```

## Commits and pushes

Writing files only changes the local working tree. Commits and pushes are performed either automatically or explicitly:

```csharp
var store = (GitStore)GitStorage.FromToken(url, token);

// explicit commit/push (group many files into a single commit)
await store.SetText("folder/a.txt", "aaa");
await store.SetText("folder/b.txt", "bbb");
await store.CommitAndPushAsync("add a and b");

// or pull the latest remote changes
await store.PullAsync();
```

Automatic commit/push on every write:

```csharp
var store = (GitStore)GitStorage.FromUrl(url, new GitStorageOptions {
   Token = token,
   AutoCommit = true,   // commit after each write
   AutoPush = true,     // push after each write
});
await store.SetText("folder/a.txt", "aaa");   // committed and pushed
```

## Versioning

Object versioning is mapped to git history:

```csharp
List<StorageObjectVersion> versions = await store.ListObjectVersions("folder/a.txt");
StorageObjectVersion old = versions.First(v => !v.IsCurrent);
await store.RestoreObjectVersion("folder/a.txt", old.VersionId);
```

## Connection Strings
To create from a connection string, first register the module when your program starts:

```csharp
GitStorage.Use();
```

Then use the following connection string:

```csharp
IStore storage = StorageFactory.FromConnectionString("git://url=https://github.com/me/repo.git;token=ghp_xxx;branch=main;root=data;localpath=/tmp/myrepo");
```

| Parameter | Required | Description |
|---|---|---|
| `url` | yes | Repository URL (HTTP/HTTPS or a local path). |
| `user` | no | HTTPS username. |
| `password` | no | HTTPS password. |
| `token` | no | Personal access token (overrides `password`). |
| `branch` | no | Branch to clone and work on. |
| `root` | no | Root sub-folder that acts as the store root. |
| `localpath` | no | Local working directory (a temporary one is used when omitted). |
