# Cluster resource

This resource allows you to create a [Unity Catalog][00].

```bicep
resource catalog 'Catalog' = {
  name: 'catalog1'
  comment: 'My first catalog'
}
```

[00]: https://docs.databricks.com/api/azure/workspace/catalogs/create
