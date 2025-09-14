using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models.Workspace;


[BicepFrontMatter("category", "Workspace")]
[BicepDocHeading("Secret", "Represents a secret stored in a Databricks secret scope.")]
[BicepDocExample(
    "Creating a string secret",
    "This example shows how to create a secret with a string value.",
    @"resource secret 'Secret' = {
  scope: 'my-secret-scope'
  key: 'database-password'
  stringValue: 'mySecretPassword123'
}
"
)]
[BicepDocExample(
    "Creating a bytes secret",
    "This example shows how to create a secret with bytes value (base64 encoded).",
    @"resource secret 'Secret' = {
  scope: 'my-secret-scope'
  key: 'certificate'
  bytesValue: 'LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0t...'
}
"
)]
[BicepDocExample(
    "API key secret",
    "This example shows how to store an API key as a secret.",
    @"resource apiKeySecret 'Secret' = {
  scope: 'api-keys'
  key: 'external-service-key'
  stringValue: 'sk-1234567890abcdef'
}
"
)]
[BicepDocCustom("Notes", @"When working with the `Secret` resource, ensure you have the extension imported in your Bicep file:

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
- Secrets are write-only; you cannot retrieve the actual secret value through the API after creation.")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Databricks Secrets API documentation][00]
- [Secret management in Databricks][01]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/secrets/putsecret
[01]: https://docs.databricks.com/security/secrets/index.html")]
[ResourceType("Secret")]
public class Secret : SecretIdentifiers
{
    // Configuration properties
    [TypeProperty("The string value of the secret.", ObjectTypePropertyFlags.None)]
    public string? StringValue { get; set; }

    [TypeProperty("If specified, value will be stored as bytes.", ObjectTypePropertyFlags.None)]
    public string? BytesValue { get; set; }

    // Read-only outputs
    [TypeProperty("The last updated timestamp of the secret.", ObjectTypePropertyFlags.ReadOnly)]
    public int LastUpdatedTimestamp { get; set; }

    [TypeProperty("The configuration reference for the secret in the format {{secrets/scope/key}}.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConfigReference { get; set; }
}

public class SecretIdentifiers
{
    [TypeProperty("The name of the secret scope.", ObjectTypePropertyFlags.Required)]
    public string Scope { get; set; } = string.Empty;

    [TypeProperty("The key name of the secret.", ObjectTypePropertyFlags.Required)]
    public string Key { get; set; } = string.Empty;
}
