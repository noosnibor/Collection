# Collection: setup guide

Do the parts in order. The first time takes about 45 minutes. After that, releasing is two clicks (Part 6).

```
 your PC ──push──► GitHub: Testing ──► Build (Release) ✔
                        │
                        └─ you click "Promote Testing to Production"
                                   │
                                   ▼
                          GitHub: Production ──► Deploy to Azure App Service
```

## Part 0: What you need

| Need | Check / get it |
| --- | --- |
| Visual Studio 2026 (or 2022) with the **ASP.NET and web development** and **Data storage and processing** workloads | Visual Studio Installer > Modify. The second one gives you SQL Server LocalDB and the database project tools. |
| .NET 10 SDK | Comes with Visual Studio. Check: `dotnet --version` shows `10.x`. |
| Git for Windows | https://git-scm.com/download/win. Check: `git --version`. |
| A GitHub account | The commands below use `noosnibor`; swap in your own if different. |
| GitHub CLI (optional, makes Parts 3 and 4 shorter) | https://cli.github.com, then run `gh auth login` once. |
| Access to the Azure portal | You need to open the App Service that will host Collection. |

First time using Git on this PC? Tell it who you are:

```
git config --global user.name "Your Name"
git config --global user.email "you@example.com"
```

## Part 1: Unzip and open

1. Unzip `Collection.zip` to a short path such as `C:\Dev\Collection`. Avoid OneDrive and Desktop folders.
2. Double-click `Collection.sln`. Visual Studio restores the NuGet packages by itself.
3. **Build > Build Solution.** It should finish with no errors.

Inside the solution you will see two folders, `src` (the app) and `database` (the SQL project), plus **Solution Items** (README, this guide, the workflows).

## Part 2: Run it on your PC

**2.1 Create the database.** Right-click `Collection.Database` > **Publish...** > pick a profile that points at LocalDB (`Database.publish.xml`) > **Publish**. Both apps' local settings use the LocalDB database named `Portal`.

`Testing.publish.xml` points at the Azure SQL server instead; use it only for Azure, and enter the password when asked.

**2.2 Add your secrets.** Right-click `Collection.Web` > **Manage User Secrets** and paste:

```json
{
  "SystemAdmin": {
    "Enabled": true,
    "Username": "sysadmin",
    "PasswordHash": "paste the hash from the step below"
  }
}
```

The System Admin is the built-in sign-in that works on a brand-new, empty database. Make its password hash from the solution folder:

```
dotnet run --project src\Collection.Web -- --hash-password "your password"
```

Paste the printed `$2a$...` text as `PasswordHash`. Only the hash is stored, never the password. Keep the password in double quotes and do not put a `$` in it, or PowerShell will try to expand it.

**2.3 Run.** Set `Collection.Web` as the startup project and press **F5**.

If the app cannot reach the database, check `ConnectionStrings:DefaultConnection` in `src/Collection.Web/appsettings.Development.json`.

## Part 3: Put it on GitHub

Open PowerShell in the folder from Part 1 and run:

```
git init -b Testing
git add .
git commit -m "Initial commit"
gh repo create noosnibor/Collection --private --source . --remote origin --push
```

*No GitHub CLI?* Create an empty repository named `Collection` on github.com (leave "Add a README" and ".gitignore" **off**), then:

```
git remote add origin https://github.com/noosnibor/Collection.git
git push -u origin Testing
```

Then check three things:

1. **Settings > Branches**: the default branch is `Testing`. (It is, because it was the first branch pushed. The Actions tab only lists workflows that exist on the default branch.)
2. **Actions** tab: **Build (Release)** is running or finished. A green tick means the code builds. If GitHub asks you to enable workflows, enable them.
3. Do **not** create a `Production` branch yourself. The first promotion in Part 5 creates it.

## Part 4: Connect GitHub to Azure

**4.1 Allow publishing from GitHub.** Azure portal > your App Service > **Settings > Configuration > General settings**. Set **SCM Basic Auth Publishing Credentials** to **On** and save. (If the *Download publish profile* button is greyed out, this is why.)

**4.2 Download the publish profile.** App Service > **Overview** > **Download publish profile**. You get a `.PublishSettings` file. Treat it like a password.

**4.3 Give GitHub the profile and the app name.** Repo > **Settings > Secrets and variables > Actions**:

| Tab | Click | Name | Value |
| --- | --- | --- | --- |
| Secrets | New repository secret | `AZURE_WEBAPP_PUBLISH_PROFILE` | Open the `.PublishSettings` file in Notepad and paste **all** of it |
| Variables | New repository variable | `AZURE_WEBAPP_NAME` | The App Service name, exactly as in Azure (for example `my-app`, not the full URL) |

Same thing with the GitHub CLI, from the repo folder in PowerShell:

```
Get-Content -Raw "C:\path\to\your-app.PublishSettings" | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE
gh variable set AZURE_WEBAPP_NAME --body "your-app-name"
```

Afterwards, delete the `.PublishSettings` file from your PC.

**4.4 Tell the Azure app its settings.** App Service > **Settings > Environment variables > App settings** > **Add** each one, then **Apply**. Secrets never go in `appsettings.json` or in GitHub.

| Name | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Your Azure SQL connection string, for example `Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=YOUR-DB;User ID=YOUR-USER;Password=YOUR-PASSWORD;Encrypt=True;` |
| `SystemAdmin__Enabled` | `true` |
| `SystemAdmin__Username` | `sysadmin` (or your choice) |
| `SystemAdmin__PasswordHash` | The hash from Part 2.2. Paste it into the Azure portal box; do not type it in PowerShell, where `$` breaks it |

Use a double underscore (`__`) where the name has a colon. Azure restarts the app when you save.

**4.5 Put the database in Azure.** In the Azure portal open your SQL server > **Networking**: tick **Allow Azure services and resources to access this server**, and add your own PC's IP address so you can publish from Visual Studio. Then right-click `Collection.Database` > **Publish...** > **Edit...** the connection to your Azure SQL server and database > **Publish**. Visual Studio asks for the password each time; it is never saved in a file.

The pipeline does **not** deploy the database. Publish `Collection.Database` yourself **before** you promote any release that changes the tables or stored procedures.

## Part 5: First release to Production

1. Repo > **Actions** > **Promote Testing to Production** > **Run workflow** > leave the branch as `Testing` > **Run workflow**.
2. Watch it: **Check the Testing build**, then **Move Testing to Production** (this creates the `Production` branch).
3. A second run, **Deploy to Azure (Production)**, starts by itself. When it is green, open your site.

## Part 6: Every release after that

1. Work in Visual Studio on the `Testing` branch (the branch name is at the bottom right). **Git > Commit All and Push**.
2. **Actions > Build (Release)** runs. Wait for the green tick.
3. If the release changes the database, publish `Collection.Database` to Azure (Part 4.5).
4. **Actions > Promote Testing to Production > Run workflow.** Azure gets the new version a few minutes later.

To undo a bad release: revert the commit on `Testing` (**Git > View Branch History**, right-click the commit, **Revert**), push, wait for the green tick, and promote again.

## Part 7: Optional safety

- **Ask for approval before each deployment.** Repo > **Settings > Environments > Production** (it appears after the first deploy) > tick **Required reviewers** and add yourself.
- Do not add "restrict who can push" protection to `Production`. The promote job pushes to it and would be blocked. The reviewer approval above is the safer gate.

## Part 8: When something goes wrong

| What you see | What it means and what to do |
| --- | --- |
| Deploy fails: *Add the AZURE_WEBAPP_NAME variable and the AZURE_WEBAPP_PUBLISH_PROFILE secret* | Part 4.3 is not done, or the name is spelled differently. Variables and secrets are separate tabs. |
| Deploy fails with 401, *publish profile*, or *basic auth* | Part 4.1 is off, or the profile is old. Turn the setting on, download a fresh profile and update the secret. |
| Promote stops: *Production has commits that are not in Testing* | Someone changed Production directly. Run `git checkout Testing`, `git pull`, `git merge origin/Production`, `git push`, wait for the green tick, then promote again. |
| Promote fails: *refusing to allow a GitHub App to create or update workflow* | You changed a file under `.github/workflows` on Testing. GitHub does not let the built-in token push that. Make the same change on `Production` by hand (edit the file on github.com, on that branch). |
| "Promote Testing to Production" is not in the Actions list | The default branch is not `Testing` (Part 3, check 1), or workflows are not enabled. |
| Build (Release) is red | Open the run and read the red step. A red build never reaches Production, because promotion re-runs it first. |
| The site shows an error page after a deploy | Almost always a missing setting from Part 4.4, or the database was not published (Part 4.5). App Service > **Monitoring > Log stream** shows the reason. |
| Visual Studio says the branch is behind | `git pull` on Testing before you push. |

## Part 9: Keep secrets out of the repo

- Passwords, app passwords and hashes live only in user secrets (your PC) and Azure App settings (the server).
- If a secret was ever typed into `appsettings.json` or committed, treat it as exposed: create a new one and revoke the old.

## Collection notes

- `Snapshot__Enabled` and `Snapshot__RetentionDays` (defaults are in `appsettings.json`) control the session recorder. Add them as App settings only if you want to change the defaults.
- In Razor files the imports are written `@using global::Collection...` (see `Components/_Imports.razor`). The `global::` is needed because there is a page called `Collection` (`Collection.razor`); without it, Razor reads `Collection.Library...` as the page class and reports errors such as CS0426 and "ToastService could not be found". Keep the prefix on any new `@using` line that starts with `Collection`.
- `TO DO LIST.txt` in the repo root was carried over from the old combined repo.
