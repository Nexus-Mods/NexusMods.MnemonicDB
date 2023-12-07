# NexusMods.App.Template

This is a template repository for `NexusMods.App.*` repositories.

## How to use

The following is a checklist of things you need to do after creating a new repository with this template.

1) Update the project settings:
   1) `Settings` -> uncheck `Wikis` (under Features)
   2) `Settings` -> check `Automatically delete head branches`
   3) `Settings` -> `Collaborators and teams` -> `Add Teams`
      - Add `NexusMods.App Admin` with role `Admin`
      - Add `NexusMods.App Developers` with role `Maintain`
      - Remove yourself as a collaborator
   4) `Settings` -> `Rules` -> `Rulesets` and add a new one:
      - Call it `Branch PR Rules`
      - Add `Repository admin` to the `Bypass list`
      - Add `Include default branch` as a target
      - Only check the following branch protections:
        - `Restrict deletions`
        - `Require signed commits`
        - `Require a pull request` before merging with **1** required approvals
        - `Require status checks to pass before merging`
        - `Block force pushes`
   5) `Settings` -> `Pages` and change `Source` to **GitHub Actions**
2) Rename the Solution and existing Projects
3) Update the docs:
    1) Open [`mkdocs.yml`](./mkdocs.yml) and update the first four fields:
        - `site_name` and `site_url`
        - `repo_name` and `repo_url`
    2) Update the docs in [`docs`](./docs). At least change the [`index.md`](./docs/index.md) file.

Finally, update this README.

## License

See [LICENSE.md](./LICENSE.md)
