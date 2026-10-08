# 发布流程 / Publishing

## 中文

项目仓库：[PB-in-GH/NightScreenGuard](https://github.com/PB-in-GH/NightScreenGuard)。

后续修改先在本地运行 `test.ps1`。需要检查界面时运行 `test.ps1 -UI`，它不会执行物理息屏。GitHub Actions 会在推送及拉取请求后进行 Windows 构建和非息屏逻辑检查；界面及真实硬件仍需要本地验证。

发布新版本时：

1. 在 `src/AssemblyInfo.cs`、`src/app.manifest` 中更新版本号，补充 `CHANGELOG.md`。
2. 运行 `build.ps1` 和相关测试，检查中英文文档及截图。
3. 将改动提交并推送到仓库，等待构建检查通过。不要上传运行日志、语言偏好、快捷方式或构建目录。
4. 为确认的提交创建版本标签，例如 `v1.1.1`；不要覆盖已经发布的标签或附件。
5. 将 `dist/` 中的程序及随附文件打成便携 ZIP；另打一个仅含已跟踪源码的 ZIP，并计算 SHA-256。
6. 创建 GitHub Release，附上双语变更说明、便携 ZIP、源码 ZIP 和校验文件。

当前程序没有代码签名。不要将 GitHub 密码或令牌放入项目文件。使用你自己的 GitHub 登录流程。

许可证为 MIT，发布时保留 LICENSE 和版权声明。

## English

Repository: [PB-in-GH/NightScreenGuard](https://github.com/PB-in-GH/NightScreenGuard).

Run `test.ps1` before submitting changes. Use `test.ps1 -UI` to check the interface without physically powering displays off. GitHub Actions builds the Windows app and runs non-display logic checks on pushes and pull requests. UI and hardware behavior still require local checks.

For a new release:

1. Update versions in `src/AssemblyInfo.cs` and `src/app.manifest`, and add a `CHANGELOG.md` entry.
2. Build, run appropriate tests and check both languages and previews.
3. Commit and push, then wait for CI success. Exclude logs, preferences, shortcuts and build directories.
4. Tag the verified commit, for example `v1.1.1`. Do not overwrite published tags or assets.
5. ZIP the portable `dist/` contents and separately archive tracked source. Calculate SHA-256 checksums.
6. Create a GitHub Release with bilingual notes, the portable and source ZIPs, and checksum file.

The application is unsigned. Never place GitHub credentials or tokens in project files. Use your own GitHub authentication flow. Preserve the MIT LICENSE and copyright notice when distributing.


## 自动发布 / Automated publishing

将版本标签先推送到 GitHub，再向 main 提交 releases/v版本号.md 双语发布说明（例如 releases/v1.1.1.md）。发布工作流会从标签源码构建、测试并创建 Release，附上便携包、源码包和 SHA-256；已经发布的版本会跳过。也可在 Actions 手动运行 Publish portable release。

Push a version tag first, then commit bilingual notes as releases/vVERSION.md on main (for example releases/v1.1.1.md). The publisher builds and tests the tagged source, creates the release with portable/source archives and checksums, and skips existing releases. It can also be started manually from Actions. The workflow only publishes from main.
