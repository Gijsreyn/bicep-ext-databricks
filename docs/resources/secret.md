---
category: "Workspace"
---

# Secret

Represents a secret stored in a Databricks secret scope.

## Example usage

### Creating a string secret

This example shows how to create a secret with a string value.

```bicep
resource secret 'Secret' = {
  scope: 'my-secret-scope'
  key: 'database-password'
  stringValue: 'mySecretPassword123'
}
```

### Creating a bytes secret

This example shows how to create a secret with bytes value (base64 encoded).

```bicep
resource secret 'Secret' = {
  scope: 'my-secret-scope'
  key: 'certificate'
  bytesValue: 'LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0t...'
}
```

### API key secret

This example shows how to store an API key as a secret.

```bicep
resource apiKeySecret 'Secret' = {
  scope: 'api-keys'
  key: 'external-service-key'
  stringValue: 'sk-1234567890abcdef'
}
```

## Argument reference

The following arguments are available:

- `key` - (Required) The key name of the secret.
- `scope` - (Required) The name of the secret scope.
- `bytesValue` - (Optional) If specified, value will be stored as bytes.
- `stringValue` - (Optional) The string value of the secret.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `configReference` - The configuration reference for the secret in the format {{secrets/scope/key}}.
- `lastUpdatedTimestamp` - The last updated timestamp of the secret.

## Notes

When working with the `Secret` resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

Please note the following important considerations when using the `Secret` resource:

- Either `stringValue` or `bytesValue` must be specified, but not both.
- The secret scope must exist before creating secrets in it. Use the `SecretScope` resource to create scopes.
- For `bytesValue`, provide the content as a base64-encoded string.
- Secrets are write-only; you cannot retrieve the actual secret value through the API after creation.

## Additional reference

For more information, see the following links:

- [Databricks Secrets API documentation][00]
- [Secret management in Databricks][01]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/secrets/putsecret
[01]: https://docs.databricks.com/security/secrets/index.html

