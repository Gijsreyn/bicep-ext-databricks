# Contributing to Bicep Databricks extension

- [Contributing to Bicep Databricks extension](#contributing-to-bicep-databricks-extension)
  - [Contribution Workflow](#contribution-workflow)
  - [Changelog](#changelog)
  - [Contributing documentation](#contributing-documentation)
  - [Adding a new resource](#adding-a-new-resource)
  - [Pester tests](#pester-tests)
  - [Code conventions](#code-conventions)

Every contribution is welcome to the Bicep Databricks extension.
We make use of GitHub issues to track reported issues by the community.
GitHub pull request are used to merge in code changes.

## Contribution Workflow

Code contributions follow a GitHub-centered workflow. To participate in
the development of the Bicep Databricks extension, you require a GitHub account first.

Then, you can follow the steps below:

1. Fork this repo by going to the project repo page and use the _Fork` button.
2. Clone down the repo to your local system

    ```bash
    git clone https://github.com/<username>/bicep-ext-databricks.git
    ```

3. Create a new branch to hold your code changes you want to make:

    ```bash
    git checkout -b branch-name
    ```

4. Work on your code and write tests if applicable.

When you are done with your work, make sure you commit the changes to
your branch. Then, you can open a pull request on this repository.

## Changelog

Every PR that adds functionality to the Bicep Databricks extension must
include a short summary of changes in the `CHANGELOG.md` file. This file
is used to determine the next version.

Changelog entries should look something like:

```plaintext
* Added new handler to perform x operation on Databricks [#123](https://github.com/Gijsreyn/issues/123)
```

Feel free to add extra content or links if needed.

## Contributing documentation

Documentation doesn't require the `CHANGELOG.md` to be updated. Every update
to the documentation is welcome! Is there an example missing? Are there spelling
mistakes? Isn't the information accurate? Feel free to go through the [docs][00].

## Adding a new resource

To add a new resource:

1. Create a new directory or add a new handler in the `Handlers` directory
2. Implement the model in the `Models` directory
3. Add Pester tests in the `tests` folder
4. Run the tests locally by executing `./build.ps1 -Configuration Release -Test`
5. Open a PR for review

> [!NOTE]
> The directory structure follows the REST API reference on the [Databricks site][01].

## Pester tests

<!-- TODO: Write about tests -->

## Code conventions

<!-- TODO: Write about conventions -->

<!-- Link reference definitions -->
[00]: ./docs/index.md
[01]: https://docs.databricks.com/api/azure/workspace/introduction