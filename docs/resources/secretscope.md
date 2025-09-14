---
category: "Workspace"
---

# SecretScope

Represents a secret scope in Databricks for organizing and managing secrets.

## Example usage

### Creating a Databricks-backed secret scope

This example shows how to create a secret scope using Databricks as the backend.

```bicep
resource secretScope 'SecretScope' = {
  scopeName: 'my-databricks-scope'
  backendType: 'DATABRICKS'
  initialManagePrincipal: 'users'
}
```

### Creating an Azure Key Vault-backed secret scope

This example shows how to create a secret scope backed by Azure Key Vault.

```bicep
resource kvSecretScope 'SecretScope' = {
  scopeName: 'azure-keyvault-scope'
  backendType: 'AZURE_KEYVAULT'
  keyVaultMetadata: {
    resourceId: '/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/myResourceGroup/providers/Microsoft.KeyVault/vaults/myKeyVault'
    dnsName: 'mykeyvault.vault.azure.net'
  }
  initialManagePrincipal: 'myuser@company.com'
}
```

### Simple secret scope

This example shows how to create a basic secret scope with minimal configuration.

```bicep
resource simpleScope 'SecretScope' = {
  scopeName: 'api-keys'
}
```

## Argument reference

The following arguments are available:

- `scopeName` - (Required) The name of the secret scope. Must consist of alphanumeric characters, dashes, underscores, and periods, and may not exceed 128 characters.
- `initialManagePrincipal` - (Optional) The principal that is initially granted MANAGE permission to the created scope.
- `keyVaultMetadata` - (Optional) Azure Key Vault metadata if using Azure Key Vault backend:
  - `dnsName` - (Required) The DNS name of the Azure Key Vault.
  - `resourceId` - (Required) The resource ID of the Azure Key Vault.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `backendType` - The backend type for the secret scope. Either 'DATABRICKS' or 'AZURE_KEYVAULT'. (Can be `DATABRICKS`, or `AZURE_KEYVAULT`)

## Notes

When working with the 'SecretScope' resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `SecretScope` resource:

- Secret scope names must be unique within the workspace
- Names can only contain alphanumeric characters, dashes, underscores, and periods (max 128 characters)
- For Azure Key Vault-backed scopes, ensure the Key Vault exists and Databricks has appropriate permissions
- The `initialManagePrincipal` can be a user email, group name, or service principal
- Once created, you can add secrets to the scope using the `Secret` resource

## Additional reference

For more information, see the following links:

- [Databricks Secret Scopes API documentation][00]
- [Secret management in Databricks][01]
- [Azure Key Vault-backed secret scopes][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/secrets/createscope
[01]: https://docs.databricks.com/security/secrets/index.html
[02]: https://docs.databricks.com/security/secrets/secret-scopes.html#azure-key-vault-backed-scopes

