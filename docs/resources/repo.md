# Repo resource

This resource allows you to [create a repository][00] in a workspace.

```bicep
resource gitRepo 'Repo' = {
  provider: 'GitHub'
  url: 'https://github.com/Azure/bicep'
  branch: 'main'
}
```

[00]: https://docs.databricks.com/api/azure/workspace/repos/create
