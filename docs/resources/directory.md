# Cluster resource

This resource allows you to [create a directory][00] in a workspace.

```bicep
resource directory 'Directory' = {
  path: '/Users/fake@example.com/directory'
}
```

[00]: https://docs.databricks.com/api/azure/workspace/workspace/mkdirs
