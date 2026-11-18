# WAFIF Collection

Collection Management Portal: Blazor Web App (InteractiveServer) for recording collections by location, currency, collection type, payment type and member type, with financial periods, reports (PDF / Excel export), user permissions and a snapshot recorder.

## Solution layout

```
Collection/
├── Collection.sln
├── src/
│   ├── Collection.Web/        Blazor Web App: pages, auth, components
│   ├── Collection.Library/    Dapper repositories, models, DTOs and services
│   └── Collection.Data/       Data-access layer (IDataAccess / DataAccess)
├── database/
│   └── Collection.Database/   SSDT project (dbo schema)
└── .github/workflows/         build.yml, promote-to-production.yml, deploy-azure.yml
```

Namespaces follow the project names: `Collection.Web.*`, `Collection.Library.*` (Model, Repository, Service, DataTransferObject) and `Collection.Data`.

## Branches and release flow

| Branch | Purpose |
| --- | --- |
| `Testing` | Where work is pushed. Every push runs **Build (Release)** (`build.yml`). This is also the repo's default branch. |
| `Production` | What is live. Never commit to it directly; it only moves when Testing is promoted. |

```
 push to Testing ──► Build (Release) ──► (you are happy) ──► run "Promote Testing to Production"
                                                                    │
                                          Production = Testing ◄────┘
                                                    │
                                                    └──► Deploy to Azure (Production)
```

1. Push your work to `Testing`. The **Build (Release)** workflow builds and publishes it, and keeps the output as a build artifact for 14 days.
2. When Testing is good, open **Actions > Promote Testing to Production > Run workflow**. It re-checks the Testing build, fast-forwards `Production` to that exact commit and starts the deployment.
3. **Deploy to Azure (Production)** publishes in Release and pushes the app to the Azure App Service. It also runs on its own if anyone pushes to `Production`.

If Production ever has a commit that Testing does not, the promote step stops with a message instead of overwriting it. Merge `Production` into `Testing`, then run it again.

If you change files under `.github/workflows/`, the built-in token cannot push those changes to `Production`. Make the workflow change on both branches by hand (or use a personal access token with the `workflow` scope).

### One-time setup in GitHub

Settings > Secrets and variables > Actions:

| Type | Name | Value |
| --- | --- | --- |
| Secret | `AZURE_WEBAPP_PUBLISH_PROFILE` | The whole contents of the App Service's publish profile (Azure portal > the App Service > *Download publish profile*) |
| Variable | `AZURE_WEBAPP_NAME` | The App Service name |

If the publish profile download is greyed out, turn on *Basic authentication* for the App Service (Configuration > General settings) first.

Optional: Settings > Environments > **Production** lets you require a reviewer to approve each deployment.

## Setup

Follow **[SETUP-GUIDE.md](SETUP-GUIDE.md)**. It covers running the app locally, the first push to GitHub, connecting GitHub to Azure, the first release, and fixes for the common errors.

## System admin password hash

The built-in System Admin is configured on the server, not in the database. To make the hash for the `SystemAdmin__PasswordHash` setting:

```
dotnet Collection.Web.dll --hash-password "your password"
```

## Notes

- The database is not deployed by the pipeline. Publish `Collection.Database` yourself when the schema changes. `Database.publish.xml` targets LocalDB and `Testing.publish.xml` targets the Azure SQL server; put the password in when Visual Studio asks, never in the file.
- `TO DO LIST.txt` is carried over from the old combined repo.
